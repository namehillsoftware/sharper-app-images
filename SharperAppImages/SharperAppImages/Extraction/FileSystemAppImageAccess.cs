using System.Text;
using BinaryTools.Elf;
using BinaryTools.Elf.Io;
using DiscUtils;
using DiscUtils.Iso9660;
using DiscUtils.SquashFs;
using PathLib;
using SharpCompress.Compressors.Xz;
using SharpCompress.Readers;
using SharperAppImages.Verification;

namespace SharperAppImages.Extraction;

public class FileSystemAppImageAccess(IAppImageExtractionConfiguration extractionConfiguration) : IAppImageAccess
{
    public async Task<bool> IsAppImage(IPath path, CancellationToken cancellationToken = default)
    {
        var type = await GetAppImageType(path, cancellationToken);
    
        return type is 1 or 2 || IsAppImagePath(path);
    }
    
    public AppImage GetExecutableAppImage(IPath appImagePath)
    {
        if (!OperatingSystem.IsWindows())
        {
            var fileInfo = appImagePath.FileInfo;
            fileInfo.UnixFileMode |= UnixFileMode.UserExecute;
        }

        return new AppImage
        {
            Path = appImagePath,
        };
    }
    
    public async Task<DesktopResources?> ExtractDesktopResources(AppImage appImage, CancellationToken cancellationToken = default)
    {
        var appImageType = await GetAppImageType(appImage.Path, cancellationToken);
        
        await using var fileStream = appImage.Path.FileInfo.OpenRead();

        IUnixFileSystem fileSystem = appImageType switch
        {
            1 => new CDReader(fileStream, true),
            2 => GetSquashyFileSystemReader(fileStream),
            _ => throw new Exception($"Unsupported architecture: {appImageType}"),
        };

        try
        {
            var desktopEntries =
                await GetStagedResources(fileSystem, "desktop", SearchOption.AllDirectories, cancellationToken);
            
            var resources = new DesktopResources
            {
                DesktopEntry = desktopEntries.FirstOrDefault(),
                Icons = await GetDesktopIcons(fileSystem, cancellationToken),
            };

            return resources;
        }
        finally
        {
            if (fileStream is IDisposable disposable)
                disposable.Dispose();
        }

        static SquashFileSystemReader GetSquashyFileSystemReader(FileStream fs)
        {
            var elfOffset = GetImageOffsetFromElf(fs);
        
            fs.Position = elfOffset;

            return new SquashFileSystemReader(
                new OffsetStream(fs),
                new SquashFileSystemReaderOptions
                {
                    GetDecompressor = (kind, _) => kind switch
                    {
                        SquashFileSystemCompressionKind.ZStd => stream =>
                            new ZstdSharp.DecompressionStream(stream, leaveOpen: true),
                        SquashFileSystemCompressionKind.Xz => stream => new XZStream(stream),
                        SquashFileSystemCompressionKind.Unknown => stream =>
                        {
                            var readerFactory =
                                ReaderFactory.Open(stream, new ReaderOptions { LeaveStreamOpen = true });
                            return readerFactory.MoveToNextEntry() ? readerFactory.OpenEntryStream() : stream;
                        },
                        _ => null,
                    }
                });
        }
    }

    private async Task<IEnumerable<IPath>> GetDesktopIcons(IUnixFileSystem mountPath, CancellationToken cancellationToken = default)
    {
        IPath[] resources =
        [
            ..await GetStagedResources(mountPath, "png", SearchOption.TopDirectoryOnly, cancellationToken),
            ..await GetStagedResources(mountPath, "svg", SearchOption.TopDirectoryOnly, cancellationToken),
            ..await GetStagedResources(mountPath, "svgz", SearchOption.TopDirectoryOnly, cancellationToken),
            ..await GetStagedResources(mountPath, "jpg", SearchOption.TopDirectoryOnly, cancellationToken),
            ..await GetStagedResources(mountPath, "jpeg", SearchOption.TopDirectoryOnly, cancellationToken),
        ];

        var directory = mountPath.GetDirectoryInfo("usr/share/icons");
        if (directory.Exists)
        {
            resources =
            [
                ..resources,
                ..await GetStagedResources(mountPath, directory, "png", SearchOption.AllDirectories, cancellationToken),
                ..await GetStagedResources(mountPath, directory, "svg", SearchOption.AllDirectories, cancellationToken),
                ..await GetStagedResources(mountPath, directory, "svgz", SearchOption.AllDirectories, cancellationToken),
                ..await GetStagedResources(mountPath, directory, "jpg", SearchOption.AllDirectories, cancellationToken),
                ..await GetStagedResources(mountPath, directory, "jpeg", SearchOption.AllDirectories, cancellationToken)
            ];
        }

        return new HashSet<IPath>(resources);
    }

    private Task<IEnumerable<IPath>> GetStagedResources(IUnixFileSystem fileSystem, string resourceExtension,
        SearchOption searchOption, CancellationToken cancellationToken = default)
    {
        return GetStagedResources(fileSystem, fileSystem.Root, resourceExtension, searchOption, cancellationToken);
    }

    private async Task<IEnumerable<IPath>> GetStagedResources(IUnixFileSystem fileSystem, DiscDirectoryInfo rootDirectory, string resourceExtension, SearchOption searchOption, CancellationToken cancellationToken = default)
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
            await using var fileStream = fileSystem.OpenFile(tmpFile.FullName, FileMode.Open, FileAccess.Read);
            await using var stagingStream = stagingPath.Open(FileMode.Create); 
            await fileStream.CopyToAsync(stagingStream, cancellationToken);
            
            resources.AddLast(stagingPath);
        }

        return resources;
    }

    private static IEnumerable<DiscFileSystemInfo> FindFilesIgnoringLinks(IUnixFileSystem fileSystemReader, DiscDirectoryInfo directory, string extension, SearchOption searchOption, CancellationToken cancellationToken = default)
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

    private static int GetImageOffsetFromElf(Stream stream)
    {
        using var reader = new EndianBinaryReader(stream, EndianBitConverter.NativeEndianness, Encoding.UTF8, true);
        var elfFile = ElfFile.ReadElfFile(reader);
        var header = elfFile.Header;

        var lastSection = elfFile.Sections[^1];
        var lastSectionEnd = (ulong)0;
        if (lastSection != null)
        {
            lastSectionEnd = lastSection.Offset + lastSection.Size;
        }
        var shSize = header.SectionHeaderOffset + (ulong)header.SectionHeaderSize * header.SectionHeaderEntryCount;
        return (int)Math.Max(lastSectionEnd, shSize);
    }
    
    private static async Task<int> GetAppImageType(IPath path, CancellationToken cancellationToken = default)
    {
        await using var appImageSteam = path.Open(FileMode.Open);
        var newPosition = appImageSteam.Seek(8, SeekOrigin.Begin);
        if (newPosition < 0) return 0;

        var magicBytes = new byte[3];
        if (await appImageSteam.ReadAsync(magicBytes, cancellationToken) != 3) return 0;

        if (magicBytes[0] != 0x41 || magicBytes[1] != 0x49) return 0;

        return magicBytes[2];
    }
    
    private static bool IsAppImagePath(IPath path) => path.Extension is ".AppImage" or ".appimage";
}