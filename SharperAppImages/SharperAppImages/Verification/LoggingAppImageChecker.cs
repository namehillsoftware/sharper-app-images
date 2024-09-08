using Microsoft.Extensions.Logging;
using PathLib;

namespace SharperAppImages.Verification;

public class LoggingAppImageChecker(ILogger<ICheckAppImages> logger, ICheckAppImages inner) : 
    DelegatingAppImageCheck(inner)
{
    public override async Task<bool> IsAppImage(IPath path)
    {
        logger.LogInformation("Checking if {path} is an AppImage.", path);
        var isAppImage = await base.IsAppImage(path);
        logger.LogInformation("Path {path} is {isAppImage}.", path, isAppImage ? "an AppImage" : "not an AppImage");
        return isAppImage;
    }
}