using Microsoft.Extensions.Logging;

namespace SharperAppImages.Registration;

public class LoggingAppRegistration(ILogger<IDesktopAppRegistration> logger, IDesktopAppRegistration inner) : IDesktopAppRegistration
{
    public async Task RegisterResources(AppImage appImage, DesktopResources desktopResources)
    {
        logger.LogInformation("Registering resources");
        await inner.RegisterResources(appImage, desktopResources);
        logger.LogInformation("Resources registered");
    }
}