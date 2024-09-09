using PathLib;

namespace SharperAppImages;

public interface IDesktopResourcesRegistration
{
    Task RegisterResources(DesktopResources desktopResources);
}