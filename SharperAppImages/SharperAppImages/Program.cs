// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Logging;
using PathLib;
using Serilog;
using Serilog.Extensions.Logging;
using SharperAppImages;
using SharperAppImages.Extraction;
using SharperAppImages.Registration;
using SharperAppImages.Verification;

await using var serilogger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

using var loggerFactory = new SerilogLoggerFactory(serilogger);

var appImageChecker = new LoggingAppImageChecker(
    loggerFactory.CreateLogger<ICheckAppImages>(),
    new AppImageChecker());

var path = new CompatPath(args[0]);
var isAppImage = await appImageChecker.IsAppImage(path);

if (!isAppImage) return -1;

var appImage = new AppImage
{
    Path = path,
};

if (!OperatingSystem.IsWindows())
{
    var fileInfo = path.FileInfo;
    fileInfo.UnixFileMode |= UnixFileMode.UserExecute;
}

using var tempDirectory = new TempDirectory();

var executionConfiguration = new ExecutionConfiguration
{
    StagingDirectory = tempDirectory,
    IconDirectory = new CompatPath("~/.local/share/icons"),
    DesktopEntryDirectory = new CompatPath("~/.local/share/applications"),
};

var appImageExtractor = new LoggingAppImageExtractor(
    loggerFactory.CreateLogger<AppImageExtractor>(),
    new AppImageExtractor(executionConfiguration));
var desktopResources = await appImageExtractor.ExtractDesktopResources(appImage);
if (desktopResources == null) return -1;

var desktopAppRegistration = new LoggingAppRegistration(
    loggerFactory.CreateLogger<LoggingAppRegistration>(),
    new DesktopAppRegistration(executionConfiguration, executionConfiguration));
await desktopAppRegistration.RegisterResources(appImage, desktopResources);

return 0;

namespace SharperAppImages
{
    internal class ExecutionConfiguration : IAppImageExtractionConfiguration, IDesktopAppLocations
    {
        public IPath StagingDirectory { get; init; }
        public IPath IconDirectory { get; init; }
        public IPath DesktopEntryDirectory { get; init; }
    }
}