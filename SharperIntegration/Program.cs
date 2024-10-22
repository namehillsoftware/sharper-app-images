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

	var executionConfiguration = new ExecutionConfiguration(new AppImageChecker())
	{
		StagingDirectory = tempDirectory,
	};

	var fileSystemAppImageAccess = new FileSystemAppImageAccess(executionConfiguration);
	ICheckAppImages appImageChecker = new LoggingAppImageChecker(
		loggerFactory.CreateLogger<ICheckAppImages>(),
		fileSystemAppImageAccess);

	if (dialogControl != null)
		appImageChecker = new InteractiveAppImageChecker(dialogControl, appImageChecker);

	var path = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
		? new CompatPath(args[0])
		: await executionConfiguration.GetProgramPathAsync(cancellationTokenSource.Token);

	var isAppImage = await appImageChecker.IsAppImage(path, cancellationTokenSource.Token);

	if (!isAppImage || cancellationTokenSource.IsCancellationRequested)
	{
		Console.ReadKey();
		return -1;
	}

	var appImage = fileSystemAppImageAccess.GetExecutableAppImage(path);

	if (cancellationTokenSource.IsCancellationRequested) return -1;

	var appImageAccessLogger = loggerFactory.CreateLogger<IAppImageExtractor>();
	IAppImageExtractor appImageAccess = new LoggingAppImageExtractor(
		appImageAccessLogger,
		new FileSystemAppImageAccess(executionConfiguration));

	if (dialogControl != null)
		appImageAccess = new InteractiveAppImageExtractor(dialogControl, appImageAccess);

	var desktopResources = await appImageAccess.ExtractDesktopResources(appImage, cancellationTokenSource.Token);
	if (desktopResources == null || cancellationTokenSource.IsCancellationRequested) return -1;

	IDesktopResourceManagement desktopAppRegistration = new LoggingResourceManagement(
		loggerFactory.CreateLogger<LoggingResourceManagement>(),
		new DesktopResourceManagement(
			executionConfiguration,
			executionConfiguration,
			executionConfiguration,
			processStarter));

	if (dialogControl != null)
		desktopAppRegistration = new InteractiveResourceManagement(
			desktopAppRegistration,
			dialogControl,
			executionConfiguration,
			processStarter);

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
		await dialogControl.DisplayWarning($"{unexpectedError} Please consult the logs for more information.");
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
	var whichProcess = Process.Start(new ProcessStartInfo("which", programName) { RedirectStandardOutput = true, });

	if (whichProcess is null) return false;

	await whichProcess.WaitForExitAsync(cancellationToken);

	var whichProcessOutput = await whichProcess.StandardOutput.ReadToEndAsync(cancellationToken);
	return whichProcess.ExitCode == 0 && !string.IsNullOrWhiteSpace(whichProcessOutput);
}

namespace SharperIntegration
{
	internal class ExecutionConfiguration(ICheckAppImages appImageChecker)
		: IAppImageExtractionConfiguration, IDesktopAppLocations, IProgramPaths
	{
		private readonly Lazy<IPath> _lazyIconDirectory = new(() => new CompatPath("~/.local/share/icons"));

		private readonly Lazy<IPath> _lazyDesktopEntryDirectory =
			new(() => new CompatPath("~/.local/share/applications"));

		private readonly Lazy<IPath> _lazyMimeConfigPath = new(() => new CompatPath("~/.config/mimeapps.list"));

		private readonly SemaphoreSlim _semaphore = new(1, 1);

		public IPath StagingDirectory { get; init; } = CompatPath.Empty;
		public IPath IconDirectory => _lazyIconDirectory.Value;
		public IPath DesktopEntryDirectory => _lazyDesktopEntryDirectory.Value;
		public IPath MimeConfigPath => _lazyMimeConfigPath.Value;

		private IPath? _programPath;

		public Task<IPath> GetProgramPathAsync(CancellationToken cancellationToken = default) =>
			GetProgramPathAsyncSynchronized(cancellationToken);

		private async Task<IPath> GetProgramPathAsyncSynchronized(CancellationToken cancellationToken = default)
		{
			if (_programPath != null) return _programPath;

			await _semaphore.WaitAsync(cancellationToken);
			try
			{
				if (_programPath != null) return _programPath;

				_programPath = await SearchForAppImageProgramPath(cancellationToken);
				return _programPath;
			}
			finally
			{
				_semaphore.Release();
			}
		}

		private async Task<IPath> SearchForAppImageProgramPath(CancellationToken cancellationToken = default)
		{
			var processPathString = Environment.GetCommandLineArgs().FirstOrDefault();
			if (processPathString != null)
			{
				var processPath = new CompatPath(processPathString);
				if (await appImageChecker.IsAppImage(processPath, cancellationToken)) return processPath;
			}

			var processId = Environment.ProcessId;

			var commandPath = await GetCommandLine(processId, cancellationToken);
			if (commandPath != null) return commandPath;

			while (processId > 0)
			{
				if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

				await using var processFileStream =
					new FileStream($"/proc/{processId}/status", FileMode.Open, FileAccess.Read);
				using var streamReader = new StreamReader(processFileStream);

				processId = 0;

				const string parentIdSearchString = "PPid:";
				while (await streamReader.ReadLineAsync(cancellationToken) is { } line)
				{
					if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

					if (!line.StartsWith(parentIdSearchString))
					{
						continue;
					}

					var processIdString = line[parentIdSearchString.Length..].Trim();
					var parentProcessId = int.Parse(processIdString);

					commandPath = await GetCommandLine(parentProcessId, cancellationToken);
					if (commandPath != null)
					{
						return commandPath;
					}

					processId = parentProcessId;
					break;
				}
			}

			throw new InvalidOperationException("Application is not executing as an AppImage.");
		}


		private async Task<IPath?> GetCommandLine(int processId, CancellationToken cancellationToken = default)
		{
			if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

			await using var commandFileStream =
				new FileStream($"/proc/{processId}/cmdline", FileMode.Open, FileAccess.Read);
			using var commandStreamReader = new StreamReader(commandFileStream);
			var command = await commandStreamReader.ReadToEndAsync(cancellationToken);
			var commands = command.Split('\0', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			command = commands.FirstOrDefault();
			if (command == null)
			{
				return null;
			}

			var processPath = new CompatPath(command);
			if (await appImageChecker.IsAppImage(processPath, cancellationToken)) return processPath;

			return null;
		}
	}
}
