using PathLib;

namespace SharperAppImages;

public static class PathExtensions
{
    public static IEnumerable<IPath> GetFiles(this IPath path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
        Directory.GetFiles(path.DirectoryInfo.FullName, searchPattern, searchOption).Select(f => f.ToPath());
    
    public static Task WriteAllBytesAsync(this IPath path, byte[] bytes) => File.WriteAllBytesAsync(path.FileInfo.FullName, bytes);
    
    public static Task WriteText(this IPath path, string text) => 
        File.WriteAllTextAsync(path.FileInfo.FullName, text);
    
    public static Task<string> ReadTextAsync(this IPath path, CancellationToken cancellationToken = default) => 
        File.ReadAllTextAsync(path.FileInfo.FullName, cancellationToken);

    public static ValueTask Touch(this IPath path) => path.Open(FileMode.CreateNew).DisposeAsync();
    
    public static void CopyTo(this IPath path, IPath destination, bool overwrite = false) =>
        path.FileInfo.CopyTo(destination.FileInfo.FullName, overwrite);
}