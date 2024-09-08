using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using PathLib;

namespace SharperAppImages;

public class TempDirectory : IDisposable, IPath
{
    private readonly Lazy<IPath> _lazyTempDir = new(() =>
    {
        var directory = new CompatPath(Path.GetTempPath()) / new CompatPath(Path.GetRandomFileName()).BasenameWithoutExtensions;
        directory.Mkdir(makeParents: true);
        return directory;
    });

    private IPath TempDir => _lazyTempDir.Value;

    void IDisposable.Dispose()
    {
        if (_lazyTempDir.IsValueCreated) _lazyTempDir.Value.Delete(true);
    }

    public string ToPosix() => TempDir.ToPosix();

    public bool IsAbsolute() => TempDir.IsAbsolute();

    public bool IsReserved() => TempDir.IsReserved();

    public IPath Join(params string[] paths) => TempDir.Join(paths);

    public IPath Join(params IPurePath[] paths) => TempDir.Join(paths);

    public IPath NormCase() => TempDir.NormCase();

    public IPath NormCase(CultureInfo currentCulture) => TempDir.NormCase(currentCulture);

    public IPath Parent() => TempDir.Parent();

    public IPath Parent(int nthParent) => TempDir.Parent(nthParent);

    public IEnumerable<IPath> Parents() => TempDir.Parents();

    public IPath Relative() => TempDir.Relative();

    public IPath RelativeTo(IPurePath parent) => TempDir.RelativeTo(parent);

    public IPath WithDirname(string newDirName) => TempDir.WithDirname(newDirName);

    public IPath WithDirname(IPurePath newDirName) => TempDir.WithDirname(newDirName);

    public IPath WithFilename(string newFilename) => TempDir.WithFilename(newFilename);

    public IPath WithExtension(string newExtension) => TempDir.WithExtension(newExtension);

    public FileSize Size => TempDir.Size;

    public FileInfo FileInfo => TempDir.FileInfo;

    public DirectoryInfo DirectoryInfo => TempDir.DirectoryInfo;

    public StatInfo Stat() => TempDir.Stat();

    public StatInfo Restat() => TempDir.Restat();

    public void Chmod(int mode) => TempDir.Chmod(mode);

    public bool Exists() => TempDir.Exists();

    public bool IsDir() => TempDir.IsDir();

    public IEnumerable<IPath> ListDir() => TempDir.ListDir();

    public IEnumerable<IPath> ListDir(string pattern) => TempDir.ListDir(pattern);

    public IEnumerable<IPath> ListDir(SearchOption scope) => TempDir.ListDir(scope);

    public IEnumerable<IPath> ListDir(string pattern, SearchOption scope) => TempDir.ListDir(pattern, scope);

    public IEnumerable<DirectoryContents<IPath>> WalkDir(Action<IOException> onError = null) => TempDir.WalkDir(onError);

    public IPath Resolve() => TempDir.Resolve();

    public bool IsFile() => TempDir.IsFile();

    public bool IsSymlink() => TempDir.IsSymlink();

    public void Lchmod(int mode) => TempDir.Lchmod(mode);

    public StatInfo Lstat() => TempDir.Lstat();

    public void Mkdir(bool makeParents = false) => TempDir.Mkdir(makeParents);

    public void Delete(bool recursive = false) => TempDir.Delete(recursive);

    public FileStream Open(FileMode mode) => TempDir.Open(mode);

    public string ReadAsText() => TempDir.ReadAsText();

    public IPath ExpandUser() => TempDir.ExpandUser();

    public IPath ExpandUser(IPath homeDir) => TempDir.ExpandUser(homeDir);

    public IPath ExpandEnvironmentVars() => TempDir.ExpandEnvironmentVars();

    public IDisposable SetCurrentDirectory() => TempDir.SetCurrentDirectory();

    IPurePath IPurePath.Join(params string[] paths) => ((IPurePath)TempDir).Join(paths);

    IPurePath IPurePath.Join(params IPurePath[] paths) => ((IPurePath)TempDir).Join(paths);

    public bool TrySafeJoin(string relativePath, [UnscopedRef] out IPurePath joined) => TempDir.TrySafeJoin(relativePath, out joined);

    public bool TrySafeJoin(IPurePath relativePath, [UnscopedRef] out IPurePath joined) => TempDir.TrySafeJoin(relativePath, out joined);

    public bool Match(string pattern) => TempDir.Match(pattern);

    IPurePath IPurePath.NormCase() => ((IPurePath)TempDir).NormCase();

    IPurePath IPurePath.NormCase(CultureInfo currentCulture) => ((IPurePath)TempDir).NormCase(currentCulture);

    IPurePath IPurePath.Parent() => ((IPurePath)TempDir).Parent();

    IPurePath IPurePath.Parent(int nthParent) => ((IPurePath)TempDir).Parent(nthParent);

    IEnumerable<IPurePath> IPurePath.Parents() => ((IPurePath)TempDir).Parents();

    public Uri ToUri() => TempDir.ToUri();

    IPurePath IPurePath.Relative() => ((IPurePath)TempDir).Relative();

    IPurePath IPurePath.RelativeTo(IPurePath parent) => ((IPurePath)TempDir).RelativeTo(parent);

    IPurePath IPurePath.WithDirname(string newDirName) => ((IPurePath)TempDir).WithDirname(newDirName);

    IPurePath IPurePath.WithDirname(IPurePath newDirName) => ((IPurePath)TempDir).WithDirname(newDirName);

    IPurePath IPurePath.WithFilename(string newFilename) => ((IPurePath)TempDir).WithFilename(newFilename);

    IPurePath IPurePath.WithExtension(string newExtension) => ((IPurePath)TempDir).WithExtension(newExtension);

    public bool HasComponents(PathComponent components) => TempDir.HasComponents(components);

    public string GetComponents(PathComponent components) => TempDir.GetComponents(components);

    public string Dirname => TempDir.Dirname;

    public string Directory => TempDir.Directory;

    public string Filename => TempDir.Filename;

    public string Basename => TempDir.Basename;

    public string BasenameWithoutExtensions => TempDir.BasenameWithoutExtensions;

    public string Extension => TempDir.Extension;

    public string[] Extensions => TempDir.Extensions;

    public string Root => TempDir.Root;

    public string Drive => TempDir.Drive;

    public string Anchor => TempDir.Anchor;

    public IEnumerable<string> Parts => TempDir.Parts;

    public bool Equals(IPath? other) => TempDir.Equals(other);

    public override string? ToString() => TempDir.ToString();

    public static IPath operator /(TempDirectory parent, string relative) => parent.Join(relative);
}