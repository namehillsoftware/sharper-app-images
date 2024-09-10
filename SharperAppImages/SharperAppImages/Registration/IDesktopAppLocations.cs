using PathLib;

namespace SharperAppImages.Registration;

public interface IDesktopAppLocations
{
    public IPath IconDirectory { get; }
    public IPath DesktopEntryDirectory { get; }
}