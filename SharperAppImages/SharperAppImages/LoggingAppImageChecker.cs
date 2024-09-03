using Microsoft.Extensions.Logging;

namespace SharperAppImages;

public class LoggingAppImageChecker(ILogger<LoggingAppImageChecker> logger, IAppImageCheck inner) : DelegatingAppImageCheck(inner)
{
    public override async Task<bool> IsAppImage(string path)
    {
        logger.LogInformation("Checking if {path} is an AppImage.", path);
        var isAppImage = await base.IsAppImage(path);
        logger.LogInformation("Path {path} is {isAppImage}.", path, isAppImage ? "an AppImage" : "not an AppImage");
        return isAppImage;
    }
}