using PathLib;
using TypeAdoption;

namespace SharperIntegration;

public partial class TempDirectory : IDisposable
{
    private readonly Lazy<IPath> _lazyTempDir = new(() =>
    {
        var directory = new CompatPath(Path.GetTempPath()) / new CompatPath(Path.GetRandomFileName()).BasenameWithoutExtensions;
        directory.Mkdir(makeParents: true);
        return directory;
    });

    [Adopt]
    private IPath TempDir => _lazyTempDir.Value;

    [Adopt(Publicly = false)]
    private IPurePath PurePath => _lazyTempDir.Value;

    void IDisposable.Dispose()
    {
        if (_lazyTempDir.IsValueCreated) _lazyTempDir.Value.Delete(true);
    }

    public override string? ToString() => TempDir.ToString();

    public static IPath operator /(TempDirectory parent, string relative) => parent.Join(relative);
}
