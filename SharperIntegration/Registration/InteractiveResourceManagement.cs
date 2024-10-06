using System.Diagnostics;
using SharperIntegration.UI;

namespace SharperIntegration.Registration;

public class InteractiveResourceManagement(
    IDesktopResourceManagement inner,
    IDialogControl dialogControl,
    IStartProcesses processes) : IDesktopResourceManagement
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

    private async Task<bool> Prompt(string title, string question, CancellationToken cancellationToken = default)
    {
        var dialogCommand = await dialogControl.GetYesNoDialogCommand(title, question, cancellationToken);
        if (dialogCommand.Length != 0)
        {
            return await processes.RunProcess(dialogCommand[0], dialogCommand[1..], cancellationToken) == 0;
        }

        Console.Write($"{question} Press y to continue...");
        var result = Console.ReadKey();
        return result.Key == ConsoleKey.Y;
    }

    private static string GetAppName(AppImage appImage) => appImage.Path.Basename;
}