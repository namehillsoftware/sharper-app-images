using System.Text;
using BinaryTools.Elf;
using BinaryTools.Elf.Io;
using DiscUtils;
using DiscUtils.Iso9660;
using DiscUtils.SquashFs;
using PathLib;
using SharpCompress.Compressors.Xz;
using SharpCompress.Readers;
using SharperIntegration.Extraction;
using ZstdSharp;

namespace SharperIntegration.Access;

public class FileSystemAppImageAccess(IAppImageExtractionConfiguration extractionConfiguration) : IAppImageAccess
{
	private static readonly HashSet<string> _resourceExtensions = ["png", "svg", "svgz", "jpg", "jpeg"];

    public async Task<bool> IsAppImage(IPath path, CancellationToken cancellationToken = default)
    {
	    if (!path.IsFile()) return false;

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

    public async Task<DesktopResources?> ExtractDesktopResources(AppImage appImage,
        CancellationToken cancellationToken = default)
    {
        var desktopEntryTask = GetDesktopEntry(appImage, cancellationToken);
        var desktopIconsTask = GetDesktopIcons(appImage, cancellationToken);

        var resources = new DesktopResources
        {
            DesktopEntry = await desktopEntryTask,
            Icons = await desktopIconsTask,
        };

        return resources;
    }

    private async Task<IPath?> GetDesktopEntry(AppImage appImage, CancellationToken cancellationToken = default)
    {
        await using var fileSystemContainer = await GetFileSystemContainer(appImage, cancellationToken);
        var fileSystem = fileSystemContainer.FileSystem;

        var resources =
            await GetStagedResources(fileSystem, ["desktop"], SearchOption.AllDirectories, cancellationToken);
        return resources.FirstOrDefault();
    }

    private async Task<IEnumerable<IPath>> GetDesktopIcons(AppImage appImage, CancellationToken cancellationToken = default)
    {
        await using var fileSystemContainer = await GetFileSystemContainer(appImage, cancellationToken);
        var fileSystem = fileSystemContainer.FileSystem;

        var resources = await GetStagedResources(
	        fileSystem,
	        _resourceExtensions,
	        SearchOption.TopDirectoryOnly,
	        cancellationToken);

        var directory = fileSystem.GetDirectoryInfo("usr/share/icons");
        if (directory.Exists)
        {
	        resources = resources.Concat(await GetStagedResources(
		        fileSystem,
		        directory,
		        _resourceExtensions,
		        SearchOption.AllDirectories,
		        cancellationToken));
        }

        return resources.ToHashSet();
    }

    private Task<IEnumerable<IPath>> GetStagedResources(IUnixFileSystem fileSystem, IEnumerable<string> resourceExtensions, SearchOption searchOption, CancellationToken cancellationToken = default)
    {
        return GetStagedResources(fileSystem, fileSystem.Root, resourceExtensions, searchOption, cancellationToken);
    }

    private async Task<IEnumerable<IPath>> GetStagedResources(
        IUnixFileSystem fileSystem,
        DiscDirectoryInfo rootDirectory,
        IEnumerable<string> resourceExtensions,
        SearchOption searchOption,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

        var tmpFiles =
            FindFilesIgnoringLinks(fileSystem, rootDirectory, resourceExtensions, searchOption, cancellationToken);
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

    private static async Task<FileSystemContainer> GetFileSystemContainer(AppImage appImage,
        CancellationToken cancellationToken = default)
    {
        var appImageType = await GetAppImageType(appImage.Path, cancellationToken);

        var fileStream = appImage.Path.FileInfo.OpenRead();

        return new FileSystemContainer(fileStream, appImageType switch
        {
            1 => new CDReader(fileStream, true),
            2 => GetSquashyFileSystemReader(fileStream),
            _ => throw new Exception($"Unsupported AppImage type: {appImageType}"),
        });

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
                            new DecompressionStream(stream, leaveOpen: true),
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

    private static IEnumerable<DiscFileSystemInfo> FindFilesIgnoringLinks(
        IUnixFileSystem fileSystemReader,
        DiscDirectoryInfo directory,
        IEnumerable<string> extensions,
        SearchOption searchOption,
        CancellationToken cancellationToken = default)
    {
	    var extensionsSet = extensions as ISet<string> ?? extensions.ToHashSet();
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

            if (info.Attributes.HasFlag(FileAttributes.Directory))
            {
	            if (searchOption == SearchOption.AllDirectories)
	            {
		            var directoryInfo = fileSystemReader.GetDirectoryInfo(info.FullName);
		            var recursiveSearch = FindFilesIgnoringLinks(
			            fileSystemReader,
			            directoryInfo,
			            extensionsSet,
			            searchOption,
			            cancellationToken);
		            foreach (var inner in recursiveSearch)
			            yield return inner;
	            }

	            continue;
            }

            if (!extensionsSet.Contains(info.Extension)) continue;

            yield return info;
        }
    }

    private static long GetImageOffsetFromElf(Stream stream)
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
        return (long)Math.Max(lastSectionEnd, shSize);
    }

    private static async Task<int> GetAppImageType(IPath path, CancellationToken cancellationToken = default)
    {
        await using var appImageSteam = path.FileInfo.OpenRead();
        var newPosition = appImageSteam.Seek(8, SeekOrigin.Begin);
        if (newPosition < 0) return 0;

        var magicBytes = new byte[3];
        if (await appImageSteam.ReadAsync(magicBytes, cancellationToken) != 3) return 0;

        if (magicBytes[0] != 0x41 || magicBytes[1] != 0x49) return 0;

        return magicBytes[2];
    }

    private static bool IsAppImagePath(IPath path) => path.Extension is ".AppImage" or ".appimage";

    private class FileSystemContainer(FileStream fileStream, IUnixFileSystem fileSystem) : IAsyncDisposable
    {
        public IUnixFileSystem FileSystem => fileSystem;

        public async ValueTask DisposeAsync()
        {
            await fileStream.DisposeAsync();

            if (fileSystem is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
