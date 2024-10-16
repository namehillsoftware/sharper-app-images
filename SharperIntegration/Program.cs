﻿using System.Diagnostics;
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
    .CreateLogger();

using var loggerFactory = new SerilogLoggerFactory(serilogger);

using var tempDirectory = new TempDirectory();

var executionConfiguration = new ExecutionConfiguration
{
    StagingDirectory = tempDirectory,
    IconDirectory = new CompatPath("~/.local/share/icons"),
    DesktopEntryDirectory = new CompatPath("~/.local/share/applications"),
    MimeConfigPath = new CompatPath("~/.config/mimeapps.list")
};

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

var processStarter = new ProcessStarter();
IDesktopResourceManagement desktopAppRegistration = new LoggingResourceManagement(
    loggerFactory.CreateLogger<LoggingResourceManagement>(),
    new DesktopResourceManagement(executionConfiguration, executionConfiguration,  processStarter));

if (!args.Contains("--force"))
{
    var dialogControl = await GetInteractionControls(cancellationTokenSource.Token);
    desktopAppRegistration = new InteractiveResourceManagement(desktopAppRegistration, dialogControl);
}

if (args.Contains("--remove"))
{
    await desktopAppRegistration.RemoveResources(appImage, desktopResources, cancellationTokenSource.Token);
}
else
{
    await desktopAppRegistration.RegisterResources(appImage, desktopResources, cancellationTokenSource.Token);
}

Console.CancelKeyPress -= OnConsoleOnCancelKeyPress;

return 0;

void OnConsoleOnCancelKeyPress(object? o, ConsoleCancelEventArgs consoleCancelEventArgs) => 
    cancellationTokenSource.Cancel();

async Task<IUserInteraction> GetInteractionControls(CancellationToken cancellationToken = default)
{
    if (await CheckIfProgramExists("zenity", cancellationToken)) return new ZenityInteraction(processStarter);
    if (await CheckIfProgramExists("kdialog", cancellationToken)) return new KDialogInteraction(processStarter);
    return new ConsoleInteraction();
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
    internal class ExecutionConfiguration : IAppImageExtractionConfiguration, IDesktopAppLocations
    {
        public IPath StagingDirectory { get; init; } = CompatPath.Empty;
        public IPath IconDirectory { get; init; } = CompatPath.Empty;
        public IPath DesktopEntryDirectory { get; init; } = CompatPath.Empty;
        public IPath MimeConfigPath { get; init; } = CompatPath.Empty;
    }
}