using Microsoft.Extensions.Logging;

namespace SharperAppImages.Registration;

public class LoggingAppRegistration(ILogger<IDesktopAppRegistration> logger, IDesktopAppRegistration inner) : IDesktopAppRegistration
{
    public async Task RegisterResources(AppImage appImage, DesktopResources desktopResources, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Registering resources");
        await inner.RegisterResources(appImage, desktopResources, cancellationToken);
        logger.LogInformation("Resources registered");
    }
}