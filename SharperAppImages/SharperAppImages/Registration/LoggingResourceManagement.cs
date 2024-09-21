using Microsoft.Extensions.Logging;

namespace SharperAppImages.Registration;

public class LoggingResourceManagement(ILogger<IDesktopResourceManagement> logger, IDesktopResourceManagement inner) : IDesktopResourceManagement
{
    public async Task RegisterResources(AppImage appImage, DesktopResources desktopResources, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Registering resources from {appImage}...", appImage.Path);
        await inner.RegisterResources(appImage, desktopResources, cancellationToken);
        logger.LogInformation("Resources registered for {appImage}.", appImage.Path);
    }

    public async Task RemoveResources(AppImage appImage, DesktopResources desktopResources, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Removing resources from {appImage}...", appImage.Path);
        await inner.RemoveResources(appImage, desktopResources, cancellationToken);
        logger.LogInformation("Resources removed for {appImage}.", appImage.Path);
    }
}