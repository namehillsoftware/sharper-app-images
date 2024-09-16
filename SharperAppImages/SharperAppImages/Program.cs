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

using var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += OnConsoleOnCancelKeyPress;

var path = new CompatPath(args[0]);
var isAppImage = await appImageChecker.IsAppImage(path, cancellationTokenSource.Token);

if (!isAppImage || cancellationTokenSource.IsCancellationRequested) return -1;

var appImage = new AppImage
{
    Path = path,
};

if (!OperatingSystem.IsWindows())
{
    var fileInfo = path.FileInfo;
    fileInfo.UnixFileMode |= UnixFileMode.UserExecute;
}

if (cancellationTokenSource.IsCancellationRequested) return -1;

using var tempDirectory = new TempDirectory();

var executionConfiguration = new ExecutionConfiguration
{
    StagingDirectory = tempDirectory,
    IconDirectory = new CompatPath("~/.local/share/icons"),
    DesktopEntryDirectory = new CompatPath("~/.local/share/applications"),
};

var appImageExtractorLogger = loggerFactory.CreateLogger<IAppImageExtractor>();
var appImageExtractor = new LoggingAppImageExtractor(
    appImageExtractorLogger,
    new SquashyAppImageExtractor(executionConfiguration));
var desktopResources = await appImageExtractor.ExtractDesktopResources(appImage, cancellationTokenSource.Token);
if (desktopResources == null || cancellationTokenSource.IsCancellationRequested) return -1;

var desktopAppRegistration = new LoggingAppRegistration(
    loggerFactory.CreateLogger<LoggingAppRegistration>(),
    new DesktopAppRegistration(executionConfiguration, executionConfiguration));
await desktopAppRegistration.RegisterResources(appImage, desktopResources, cancellationTokenSource.Token);

Console.CancelKeyPress -= OnConsoleOnCancelKeyPress;

return 0;

void OnConsoleOnCancelKeyPress(object? o, ConsoleCancelEventArgs consoleCancelEventArgs) => 
    cancellationTokenSource.Cancel();

namespace SharperAppImages
{
    internal class ExecutionConfiguration : IAppImageExtractionConfiguration, IDesktopAppLocations
    {
        public IPath StagingDirectory { get; init; } = CompatPath.Empty;
        public IPath IconDirectory { get; init; } = CompatPath.Empty;
        public IPath DesktopEntryDirectory { get; init; } = CompatPath.Empty;
    }
}