using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PathLib;
using Serilog;
using Serilog.Extensions.Logging;
using SharperIntegration;
using SharperIntegration.Access;
using SharperIntegration.Extraction;
using SharperIntegration.Registration;
using SharperIntegration.UI;
using SharperIntegration.Verification;

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += OnConsoleOnCancelKeyPress;

await using var serilogger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.LocalSyslog(appName: "SharperIntegration")
    .CreateLogger();

using var loggerFactory = new SerilogLoggerFactory(serilogger);

var processStarter = new ProcessStarter(loggerFactory.CreateLogger<ProcessStarter>());
var dialogControl = await GetInteractionControls(cancellationTokenSource.Token);

try
{
	using var tempDirectory = new TempDirectory();

	var executionConfiguration = new ExecutionConfiguration { StagingDirectory = tempDirectory, };

	var fileSystemAppImageAccess = new FileSystemAppImageAccess(executionConfiguration);
	var appImageChecker = new LoggingAppImageChecker(
		loggerFactory.CreateLogger<ICheckAppImages>(),
		fileSystemAppImageAccess);

	var fileName = args.Length > 0 ? args[0] : Environment.CommandLine;

	if (string.IsNullOrWhiteSpace(fileName)) return -1;

	var path = new CompatPath(fileName);
	var isAppImage = await appImageChecker.IsAppImage(path, cancellationTokenSource.Token);

	if (!isAppImage || cancellationTokenSource.IsCancellationRequested) return -1;

	var appImage = fileSystemAppImageAccess.GetExecutableAppImage(path);

	if (cancellationTokenSource.IsCancellationRequested) return -1;

	var appImageAccessLogger = loggerFactory.CreateLogger<IAppImageExtractor>();
	var appImageAccess = new LoggingAppImageExtractor(
		appImageAccessLogger,
		new FileSystemAppImageAccess(executionConfiguration));

	var desktopResources = await appImageAccess.ExtractDesktopResources(appImage, cancellationTokenSource.Token);
	if (desktopResources == null || cancellationTokenSource.IsCancellationRequested) return -1;

	IDesktopResourceManagement desktopAppRegistration = new LoggingResourceManagement(
		loggerFactory.CreateLogger<LoggingResourceManagement>(),
		new DesktopResourceManagement(executionConfiguration, executionConfiguration, executionConfiguration,
			processStarter));

	if (dialogControl != null)
		desktopAppRegistration = new InteractiveResourceManagement(desktopAppRegistration, dialogControl,
			executionConfiguration, processStarter);

	if (args.Contains("--remove"))
	{
		await desktopAppRegistration.RemoveResources(appImage, desktopResources, cancellationTokenSource.Token);
	}
	else if (args.Contains("--update"))
	{
		await desktopAppRegistration.UpdateImage(appImage, cancellationTokenSource.Token);
	}
	else
	{
		await desktopAppRegistration.RegisterResources(appImage, desktopResources, cancellationTokenSource.Token);
	}
}
catch (Exception e) when (e is not TaskCanceledException)
{
	const string unexpectedError = "An unexpected error occurred while running Sharper Integration.";

	loggerFactory.CreateLogger<Program>().LogError(e, unexpectedError);

	if (cancellationTokenSource.IsCancellationRequested) return -1;

	if (dialogControl != null)
	{
		await dialogControl.DisplayWarning(
			"Unexpected Error",
			$"{unexpectedError} Please consult the logs for more information.");
	}
}
finally
{
	Console.CancelKeyPress -= OnConsoleOnCancelKeyPress;
}

return 0;

void OnConsoleOnCancelKeyPress(object? o, ConsoleCancelEventArgs consoleCancelEventArgs) =>
    cancellationTokenSource.Cancel();

async Task<IUserInteraction?> GetInteractionControls(CancellationToken cancellationToken = default)
{
	if (args.Contains("--non-interactive")) return null;
    if (await CheckIfProgramExists("zenity", cancellationToken)) return new ZenityInteraction(processStarter);
    if (await CheckIfProgramExists("kdialog", cancellationToken)) return new KDialogInteraction(processStarter);
    return Console.WindowHeight > 0 ? new ConsoleInteraction() : null;
}

static async Task<bool> CheckIfProgramExists(string programName, CancellationToken cancellationToken = default)
{
    var whichProcess = Process.Start(new ProcessStartInfo("which", programName)
    {
        RedirectStandardOutput = true,
    });

    if (whichProcess is null) return false;

    await whichProcess.WaitForExitAsync(cancellationToken);

    var whichProcessOutput = await whichProcess.StandardOutput.ReadToEndAsync(cancellationToken);
    return whichProcess.ExitCode == 0 && !string.IsNullOrWhiteSpace(whichProcessOutput);
}

namespace SharperIntegration
{
    internal class ExecutionConfiguration : IAppImageExtractionConfiguration, IDesktopAppLocations, IProgramPaths
    {
        private readonly Lazy<IPath> _lazyIconDirectory = new(() => new CompatPath("~/.local/share/icons"));
        private readonly Lazy<IPath> _lazyDesktopEntryDirectory = new(() => new CompatPath("~/.local/share/applications"));
        private readonly Lazy<IPath> _lazyMimeConfigPath = new(() => new CompatPath("~/.config/mimeapps.list"));
        private readonly Lazy<IPath> _lazyProgramPath = new(() =>
            new CompatPath(Environment.CommandLine.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).First()));

        public IPath StagingDirectory { get; init; } = CompatPath.Empty;
        public IPath IconDirectory => _lazyIconDirectory.Value;
        public IPath DesktopEntryDirectory => _lazyDesktopEntryDirectory.Value;
        public IPath MimeConfigPath => _lazyMimeConfigPath.Value;
        public IPath ProgramPath => _lazyProgramPath.Value;
    }
}
