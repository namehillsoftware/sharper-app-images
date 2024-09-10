using PathLib;

namespace SharperAppImages;

public static class PathExtensions
{
    public static IEnumerable<IPath> GetFiles(this IPath path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
        Directory.GetFiles(path.DirectoryInfo.FullName, searchPattern, searchOption).Select(f => f.ToPath());
    
    public static Task WriteAllBytesAsync(this IPath path, byte[] bytes) => File.WriteAllBytesAsync(path.FileInfo.FullName, bytes);
    
    public static Task WriteText(this IPath path, string text) => File.WriteAllTextAsync(path.FileInfo.FullName, text);

    public static ValueTask Touch(this IPath path) => path.Open(FileMode.CreateNew).DisposeAsync();
}