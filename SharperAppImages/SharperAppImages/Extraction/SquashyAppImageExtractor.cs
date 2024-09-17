using System.Diagnostics;
using System.Text;
using DiscUtils;
using DiscUtils.SquashFs;
using PathLib;
using SharpCompress.Compressors.Xz;
using SharpCompress.Readers;

namespace SharperAppImages.Extraction;

public class SquashyAppImageExtractor(IAppImageExtractionConfiguration extractionConfiguration) : IAppImageExtractor
{
    public async Task<DesktopResources?> ExtractDesktopResources(AppImage appImage, CancellationToken cancellationToken = default)
    {
        var offset = await GetImageOffset(appImage, cancellationToken);
        
        if (offset == null) return null;

        await using var fileStream = appImage.Path.FileInfo.OpenRead();
        fileStream.Position = offset.Value;
        using var squashy = new SquashFileSystemReader(
            new OffsetStream(fileStream),
            new SquashFileSystemReaderOptions
            {
                GetDecompressor = (kind, options) => kind switch {
                    SquashFileSystemCompressionKind.ZStd => stream => new ZstdSharp.DecompressionStream(stream, leaveOpen: true),
                    SquashFileSystemCompressionKind.Xz => stream => new XZStream(stream),
                    SquashFileSystemCompressionKind.Unknown => stream =>
                    {
                        var readerFactory = ReaderFactory.Open(stream, new ReaderOptions { LeaveStreamOpen = true });
                        return readerFactory.MoveToNextEntry() ? readerFactory.OpenEntryStream() : stream;
                    },
                    _ => null,
                }
            });

        var desktopEntries = GetStagedResources(squashy, "desktop", SearchOption.AllDirectories, cancellationToken);

        var resources = new DesktopResources
        {
            DesktopEntry = desktopEntries.FirstOrDefault(),
            Icons = GetDesktopIcons(squashy, cancellationToken),
        };

        return resources;
    }

    private IEnumerable<IPath> GetDesktopIcons(SquashFileSystemReader mountPath, CancellationToken cancellationToken = default)
    {
        IEnumerable<IPath> resources =
        [
            ..GetStagedResources(mountPath, "png", SearchOption.TopDirectoryOnly, cancellationToken),
            ..GetStagedResources(mountPath, "svg", SearchOption.TopDirectoryOnly, cancellationToken),
            ..GetStagedResources(mountPath, "svgz", SearchOption.TopDirectoryOnly, cancellationToken),
            ..GetStagedResources(mountPath, "jpg", SearchOption.TopDirectoryOnly, cancellationToken),
            ..GetStagedResources(mountPath, "jpeg", SearchOption.TopDirectoryOnly, cancellationToken),
        ];

        var directory = mountPath.GetDirectoryInfo(@"usr/share/icons");
        if (directory.Exists)
        {
            resources =
            [
                ..resources,
                ..GetStagedResources(mountPath, directory, "png", SearchOption.AllDirectories, cancellationToken),
                ..GetStagedResources(mountPath, directory, "svg", SearchOption.AllDirectories, cancellationToken),
                ..GetStagedResources(mountPath, directory, "svgz", SearchOption.AllDirectories, cancellationToken),
                ..GetStagedResources(mountPath, directory, "jpg", SearchOption.AllDirectories, cancellationToken),
                ..GetStagedResources(mountPath, directory, "jpeg", SearchOption.AllDirectories, cancellationToken)
            ];
        }

        return new HashSet<IPath>(resources);
    }

    private IEnumerable<IPath> GetStagedResources(SquashFileSystemReader fileSystem, string resourceExtension,
        SearchOption searchOption, CancellationToken cancellationToken = default)
    {
        return GetStagedResources(fileSystem, fileSystem.Root, resourceExtension, searchOption, cancellationToken);
    }

    private IEnumerable<IPath> GetStagedResources(SquashFileSystemReader fileSystem, DiscDirectoryInfo rootDirectory, string resourceExtension, SearchOption searchOption, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

        var tmpFiles = FindFilesIgnoringLinks(fileSystem, rootDirectory, resourceExtension, searchOption, cancellationToken);
        var resources = new LinkedList<IPath>();
        var stagingDirectory = extractionConfiguration.StagingDirectory;
        foreach (var tmpFile in tmpFiles)
        {
            if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

            var stagingPath = stagingDirectory / tmpFile.FullName;
            stagingPath.Parent().Mkdir(makeParents: true);
            using var fileStream = fileSystem.OpenFile(tmpFile.FullName, FileMode.Open, FileAccess.Read);
            fileStream.CopyToAsync(stagingPath.Open(FileMode.Create), cancellationToken);
            
            resources.AddLast(stagingPath);
        }

        return resources;
    }

    private static IEnumerable<DiscFileSystemInfo> FindFilesIgnoringLinks(SquashFileSystemReader fileSystemReader, DiscDirectoryInfo directory, string extension, SearchOption searchOption, CancellationToken cancellationToken = default)
    {
        var infos = directory.GetFileSystemInfos();
        foreach (var info in infos)
        {
            if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

            try
            {
                if (fileSystemReader.GetUnixFileInfo(info.FullName).FileType == UnixFileType.Link) continue;
            }
            catch (NotImplementedException)
            {
                // Also a symlink /shrug
                continue;
            }
            catch (Exception e) when (e.Message == "Data Error")
            {
                continue;
            }

            if (searchOption == SearchOption.AllDirectories && info.Attributes.HasFlag(FileAttributes.Directory))
            {
                var directoryInfo = fileSystemReader.GetDirectoryInfo(info.FullName);
                foreach (var inner in FindFilesIgnoringLinks(fileSystemReader, directoryInfo, extension, searchOption, cancellationToken))
                    yield return inner;
            }

            if (info.Extension != extension) continue;

            yield return info;
        }
    }

    private static async Task<int?> GetImageOffset(AppImage appImage, CancellationToken cancellationToken = default)
    {
        using var process = Process.Start(new ProcessStartInfo(appImage.Path.FileInfo.FullName, ["--appimage-offset"])
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = Encoding.UTF8,
        });
        
        if (process == null) return null;

        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            throw new UnexpectedAppImageExecutionCode(
                process.ExitCode,
                await process.StandardError.ReadToEndAsync(cancellationToken),
                await process.StandardOutput.ReadToEndAsync(cancellationToken));
        }
        
        var standardOutput = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var offset = int.Parse(standardOutput);
        
        return offset;
    }
}