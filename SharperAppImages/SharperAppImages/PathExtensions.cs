using PathLib;

namespace SharperAppImages;

public static class PathExtensions
{
    public static IEnumerable<IPath> GetFiles(this IPath path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
        Directory.GetFiles(path.DirectoryInfo.FullName, searchPattern, searchOption).Select(f => f.ToPath());
}