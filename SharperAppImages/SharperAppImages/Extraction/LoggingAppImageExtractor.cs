using Microsoft.Extensions.Logging;
using SharperAppImages.Verification;

namespace SharperAppImages.Extraction;

public class LoggingAppImageExtractor(ILogger<IAppImageExtractor> logger, IAppImageExtractor inner) : IAppImageExtractor
{
    public async Task<DesktopResources?> ExtractDesktopResources(AppImage appImage, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Extracting desktop resources for {appImage}.", appImage.Path);
        var resources = await inner.ExtractDesktopResources(appImage, cancellationToken);
        logger.LogInformation("Desktop resources extracted for {appImage}.", appImage.Path);
        return resources;
    }
}