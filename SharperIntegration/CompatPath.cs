using PathLib;
using TypeAdoption;

namespace SharperIntegration;

public partial class CompatPath(IPath fromPath)
{
	[Adopt] private readonly IPath _fromPath = fromPath;

	[Adopt(Publicly = false)] private IPurePath PurePath => _fromPath;

    public CompatPath(string path)
        : this(Paths.Create(path))
    {
    }

    public CompatPath(IPurePath path) : this(path as IPath ?? Paths.Create(path.ToPosix()))
    {
    }

    public static IPath Empty { get; } = new CompatPath(string.Empty);

    public bool IsAbsolute() => Path.IsPathRooted(_fromPath.ToString());

    public CompatPath Join(params string[] paths) => new(Path.Combine([_fromPath.ToString() ?? string.Empty, ..paths]));

    IPath IPath.Join(params string[] paths) => _fromPath.Join(paths);

    IPurePath IPurePath.Join(params IPurePath[] paths) => PurePath.Join(paths);

    public bool Exists() => Path.Exists(_fromPath.ToString());

    public override string? ToString() => _fromPath.ToString();

    public static CompatPath operator /(CompatPath parent, string relative) => parent.Join(relative);
    public static IPath operator /(CompatPath parent, IPath relative) => parent.Join(relative);
}

public static class CompatPathExtensions
{
    public static CompatPath ToPath(this string path) => new(path);
}
