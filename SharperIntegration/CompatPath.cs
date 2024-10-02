using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using PathLib;

namespace SharperIntegration;

public class CompatPath(IPath fromPath) : IPath
{
    public CompatPath(string path)
        : this(Paths.Create(path))
    {
    }

    public CompatPath(IPurePath path) : this(path as IPath ?? Paths.Create(path.ToPosix()))
    {
    }
    
    public static IPath Empty { get; } = new CompatPath(string.Empty);

    public string ToPosix()
    {
        return fromPath.ToPosix();
    }

    public bool IsAbsolute()
    {
        return Path.IsPathRooted(ToString());
    }

    public bool IsReserved()
    {
        return fromPath.IsReserved();
    }
    
    public CompatPath Join(params string[] paths) => new(Path.Combine([ToString() ?? string.Empty, ..paths]));

    IPath IPath.Join(params string[] paths)
    {
        return fromPath.Join(paths);
    }

    public IPath Join(params IPurePath[] paths)
    {
        return fromPath.Join(paths);
    }

    public IPath NormCase()
    {
        return fromPath.NormCase();
    }

    public IPath NormCase(CultureInfo currentCulture)
    {
        return fromPath.NormCase(currentCulture);
    }

    public IPath Parent()
    {
        return fromPath.Parent();
    }

    public IPath Parent(int nthParent)
    {
        return fromPath.Parent(nthParent);
    }

    public IEnumerable<IPath> Parents()
    {
        return fromPath.Parents();
    }

    public IPath Relative()
    {
        return fromPath.Relative();
    }

    public IPath RelativeTo(IPurePath parent)
    {
        return fromPath.RelativeTo(parent);
    }

    public IPath WithDirname(string newDirName)
    {
        return fromPath.WithDirname(newDirName);
    }

    public IPath WithDirname(IPurePath newDirName)
    {
        return fromPath.WithDirname(newDirName);
    }

    public IPath WithFilename(string newFilename)
    {
        return fromPath.WithFilename(newFilename);
    }

    public IPath WithExtension(string newExtension)
    {
        return fromPath.WithExtension(newExtension);
    }

    public FileSize Size => fromPath.Size;

    public FileInfo FileInfo => fromPath.FileInfo;

    public DirectoryInfo DirectoryInfo => fromPath.DirectoryInfo;

    public StatInfo Stat()
    {
        return fromPath.Stat();
    }

    public StatInfo Restat()
    {
        return fromPath.Restat();
    }

    public void Chmod(int mode)
    {
        fromPath.Chmod(mode);
    }

    public bool Exists()
    {
        return Path.Exists(fromPath.ToString());
    }

    public bool IsDir()
    {
        return fromPath.IsDir();
    }

    public IEnumerable<IPath> ListDir()
    {
        return fromPath.ListDir();
    }

    public IEnumerable<IPath> ListDir(string pattern)
    {
        return fromPath.ListDir(pattern);
    }

    public IEnumerable<IPath> ListDir(SearchOption scope)
    {
        return fromPath.ListDir(scope);
    }

    public IEnumerable<IPath> ListDir(string pattern, SearchOption scope)
    {
        return fromPath.ListDir(pattern, scope);
    }

    public IEnumerable<DirectoryContents<IPath>> WalkDir(Action<IOException>? onError = null)
    {
        return fromPath.WalkDir(onError);
    }

    public IPath Resolve()
    {
        return fromPath.Resolve();
    }

    public bool IsFile()
    {
        return fromPath.IsFile();
    }

    public bool IsSymlink()
    {
        return fromPath.IsSymlink();
    }

    public void Lchmod(int mode)
    {
        fromPath.Lchmod(mode);
    }

    public StatInfo Lstat()
    {
        return fromPath.Lstat();
    }

    public void Mkdir(bool makeParents = false)
    {
        fromPath.Mkdir(makeParents);
    }

    public void Delete(bool recursive = false)
    {
        fromPath.Delete(recursive);
    }

    public FileStream Open(FileMode mode)
    {
        return fromPath.Open(mode);
    }

    public string ReadAsText()
    {
        return fromPath.ReadAsText();
    }

    public IPath ExpandUser()
    {
        return fromPath.ExpandUser();
    }

    public IPath ExpandUser(IPath homeDir)
    {
        return fromPath.ExpandUser(homeDir);
    }

    public IPath ExpandEnvironmentVars()
    {
        return fromPath.ExpandEnvironmentVars();
    }

    public IDisposable SetCurrentDirectory()
    {
        return fromPath.SetCurrentDirectory();
    }

    IPurePath IPurePath.Join(params string[] paths)
    {
        return ((IPurePath)fromPath).Join(paths);
    }

    IPurePath IPurePath.Join(params IPurePath[] paths)
    {
        return ((IPurePath)fromPath).Join(paths);
    }

    public bool TrySafeJoin(string relativePath, [UnscopedRef] out IPurePath joined)
    {
        return fromPath.TrySafeJoin(relativePath, out joined);
    }

    public bool TrySafeJoin(IPurePath relativePath, [UnscopedRef] out IPurePath joined)
    {
        return fromPath.TrySafeJoin(relativePath, out joined);
    }

    public bool Match(string pattern)
    {
        return fromPath.Match(pattern);
    }

    IPurePath IPurePath.NormCase()
    {
        return ((IPurePath)fromPath).NormCase();
    }

    IPurePath IPurePath.NormCase(CultureInfo currentCulture)
    {
        return ((IPurePath)fromPath).NormCase(currentCulture);
    }

    IPurePath IPurePath.Parent()
    {
        return ((IPurePath)fromPath).Parent();
    }

    IPurePath IPurePath.Parent(int nthParent)
    {
        return ((IPurePath)fromPath).Parent(nthParent);
    }

    IEnumerable<IPurePath> IPurePath.Parents()
    {
        return ((IPurePath)fromPath).Parents();
    }

    public Uri ToUri()
    {
        return fromPath.ToUri();
    }

    IPurePath IPurePath.Relative()
    {
        return ((IPurePath)fromPath).Relative();
    }

    IPurePath IPurePath.RelativeTo(IPurePath parent)
    {
        return ((IPurePath)fromPath).RelativeTo(parent);
    }

    IPurePath IPurePath.WithDirname(string newDirName)
    {
        return ((IPurePath)fromPath).WithDirname(newDirName);
    }

    IPurePath IPurePath.WithDirname(IPurePath newDirName)
    {
        return ((IPurePath)fromPath).WithDirname(newDirName);
    }

    IPurePath IPurePath.WithFilename(string newFilename)
    {
        return ((IPurePath)fromPath).WithFilename(newFilename);
    }

    IPurePath IPurePath.WithExtension(string newExtension)
    {
        return ((IPurePath)fromPath).WithExtension(newExtension);
    }

    public bool HasComponents(PathComponent components)
    {
        return fromPath.HasComponents(components);
    }

    public string GetComponents(PathComponent components)
    {
        return fromPath.GetComponents(components);
    }

    public string Dirname => fromPath.Dirname;

    public string Directory => fromPath.Directory;

    public string Filename => fromPath.Filename;

    public string Basename => fromPath.Basename;

    public string BasenameWithoutExtensions => fromPath.BasenameWithoutExtensions;

    public string Extension => fromPath.Extension;

    public string[] Extensions => fromPath.Extensions;

    public string Root => fromPath.Root;

    public string Drive => fromPath.Drive;

    public string Anchor => fromPath.Anchor;

    public IEnumerable<string> Parts => fromPath.Parts;

    public bool Equals(IPath? other)
    {
        return fromPath.Equals(other);
    }

    public override string? ToString() => fromPath.ToString();

    public static CompatPath operator /(CompatPath parent, string relative) => parent.Join(relative);
    public static IPath operator /(CompatPath parent, IPath relative) => parent.Join(relative);
}

public static class CompatPathExtensions
{
    public static CompatPath ToPath(this string path) => new(path);
}