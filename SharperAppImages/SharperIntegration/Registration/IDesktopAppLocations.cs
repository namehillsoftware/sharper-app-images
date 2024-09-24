using PathLib;

namespace SharperIntegration.Registration;

public interface IDesktopAppLocations
{
    public IPath IconDirectory { get; }
    public IPath DesktopEntryDirectory { get; }
}