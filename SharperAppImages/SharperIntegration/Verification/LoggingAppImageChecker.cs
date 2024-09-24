using Microsoft.Extensions.Logging;
using PathLib;

namespace SharperIntegration.Verification;

public class LoggingAppImageChecker(ILogger<ICheckAppImages> logger, ICheckAppImages inner) : 
    DelegatingAppImageChecker(inner)
{
    public override async Task<bool> IsAppImage(IPath path, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Checking if {path} is an AppImage...", path);
        var isAppImage = await base.IsAppImage(path, cancellationToken);
        logger.LogInformation("Path {path} is {isAppImage}.", path, isAppImage ? "an AppImage" : "not an AppImage");
        return isAppImage;
    }
}