using System.Diagnostics;

namespace SharperIntegration.Registration;

public class InteractiveDesktopRegistration(IDesktopResourceManagement inner) : IDesktopResourceManagement
{
    private const string RegistrationQuestion = "Integrate {0} into the Desktop?";
    private const string RemovalQuestion = "Remove {0} from Desktop?";
    
    public async Task RegisterResources(AppImage appImage, DesktopResources desktopResources, CancellationToken cancellationToken = default)
    {
        var promptResult = await Prompt(
            "Integrate into Desktop?",
            string.Format(RegistrationQuestion, GetAppName(appImage)),
            cancellationToken);
        if (promptResult)
            await inner.RegisterResources(appImage, desktopResources, cancellationToken);
    }

    public async Task RemoveResources(AppImage appImage, DesktopResources desktopResources, CancellationToken cancellationToken = default)
    {
        var promptResult = await Prompt(
            "Remove from Desktop?",
            string.Format(RemovalQuestion, GetAppName(appImage)),
            cancellationToken);
        if (promptResult)
            await inner.RemoveResources(appImage, desktopResources, cancellationToken);
    }

    private static async Task<bool> Prompt(string title, string question, CancellationToken cancellationToken = default)
    {
        if (await CheckIfProgramExists("zenity"))
        {
            var process = Process.Start(
                "zenity",
                [
                    "--question",
                    $"--title={title}",
                    $"--text={question}",
                ]);
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0;
        }

        if (await CheckIfProgramExists("kdialog"))
        {
            var process = Process.Start(
                "kdialog",
                [
                    "--question",
                    $"--title={title}",
                    $"--yesno={question}",
                ]);
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0;
        }

        Console.Write($"{question} Press y to continue...");
        var result = Console.ReadKey();
        return result.Key == ConsoleKey.Y;
    }

    private static async Task<bool> CheckIfProgramExists(string programName)
    {
        var whichProcess = Process.Start(new ProcessStartInfo("which", programName)
        {
            RedirectStandardOutput = true,
        });
        
        if (whichProcess is null) return false;
        
        await whichProcess.WaitForExitAsync();
        
        var whichProcessOutput = await whichProcess.StandardOutput.ReadToEndAsync();
        return whichProcess.ExitCode == 0 && !string.IsNullOrWhiteSpace(whichProcessOutput);
    }
    
    private static string GetAppName(AppImage appImage) => appImage.Path.Basename;
}