namespace SharperIntegration.Registration;

public interface IDesktopResourceManagement
{
    Task RegisterResources(AppImage appImage, DesktopResources desktopResources, CancellationToken cancellationToken = default);
    
    Task RemoveResources(AppImage appImage, DesktopResources desktopResources, CancellationToken cancellationToken = default);
}