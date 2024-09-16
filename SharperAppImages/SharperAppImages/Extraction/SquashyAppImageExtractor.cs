using System.Diagnostics;
using System.Text;
using DiscUtils;
using DiscUtils.SquashFs;
using PathLib;

namespace SharperAppImages.Extraction;

public class SquashyAppImageExtractor(IAppImageExtractionConfiguration extractionConfiguration) : IAppImageExtractor
{
    public async Task<DesktopResources?> ExtractDesktopResources(AppImage appImage, CancellationToken cancellationToken = default)
    {
        var offset = await GetImageOffset(appImage, cancellationToken);
        
        if (offset == null) return null;

        await using var fileStream = appImage.Path.FileInfo.OpenRead();
        fileStream.Position = offset.Value;
        using var squashy = new SquashFileSystemReader(new OffsetStream(fileStream));

        var desktopEntries = GetResources(squashy, "desktop", cancellationToken);

        var resources = new DesktopResources
        {
            DesktopEntry = desktopEntries.FirstOrDefault(),
            Icons = GetDesktopIcons(squashy, cancellationToken).ToArray(),
        };

        return resources;
    }

    private IEnumerable<IPath> GetDesktopIcons(SquashFileSystemReader mountPath, CancellationToken cancellationToken = default)
    {
        IEnumerable<IPath> resources =
        [
            ..GetResources(mountPath, "png", cancellationToken),
            ..GetResources(mountPath, "svg", cancellationToken),
            ..GetResources(mountPath, "svgz", cancellationToken),
            ..GetResources(mountPath, "jpg", cancellationToken),
            ..GetResources(mountPath, "jpeg", cancellationToken),
        ];

        return resources.Distinct();
    }

    private static async Task<int?> GetImageOffset(AppImage appImage,
        CancellationToken cancellationToken = default)
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

    private List<IPath> GetResources(SquashFileSystemReader fileSystem, string resourceExtension, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

        var tmpFiles = FindFilesIgnoringLinks(fileSystem, fileSystem.Root, resourceExtension, cancellationToken).ToArray();
        var resources = new List<IPath>(tmpFiles.Length);
        var stagingDirectory = extractionConfiguration.StagingDirectory;
        foreach (var tmpFile in tmpFiles)
        {
            if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

            var stagingPath = stagingDirectory / tmpFile.FullName;
            stagingPath.Parent().Mkdir(makeParents: true);
            using var fileStream = fileSystem.OpenFile(tmpFile.FullName, FileMode.Open, FileAccess.Read);
            fileStream.CopyToAsync(stagingPath.Open(FileMode.Create), cancellationToken);
            
            resources.Add(stagingPath);
        }

        return resources;
    }

    private static IEnumerable<DiscFileSystemInfo> FindFilesIgnoringLinks(SquashFileSystemReader fileSystemReader, DiscDirectoryInfo directory, string extension, CancellationToken cancellationToken = default)
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

            if (info.Attributes.HasFlag(FileAttributes.Directory))
            {
                var directoryInfo = fileSystemReader.GetDirectoryInfo(info.FullName);
                foreach (var inner in FindFilesIgnoringLinks(fileSystemReader, directoryInfo, extension, cancellationToken))
                    yield return inner;
            }

            if (info.Extension != extension) continue;

            yield return info;
        }
    }
}