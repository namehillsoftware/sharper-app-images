using SharperIntegration.UI;

namespace SharperIntegration.Registration;

public class InteractiveResourceManagement(
    IDesktopResourceManagement inner, 
    IUserInteraction userInteraction,
    IProgramPaths programPaths,
    IStartProcesses processes) : IDesktopResourceManagement
{
    private const string RegistrationQuestion = "Integrate {0} into the Desktop?";
    private const string RemovalQuestion = "Remove {0} from Desktop?";
    private const string UpdateQuestion = "Update {0}?";
    
    public async Task RegisterResources(AppImage appImage, DesktopResources desktopResources, CancellationToken cancellationToken = default)
    {
        var question = string.Format(RegistrationQuestion, GetAppName(appImage));
        var promptResult = await userInteraction.PromptYesNo("Integrate into Desktop?", question, cancellationToken);
        if (promptResult)
            await inner.RegisterResources(appImage, desktopResources, cancellationToken);
    }

    public async Task UpdateImage(AppImage appImage, CancellationToken cancellationToken = default)
    {
        var question = string.Format(UpdateQuestion, GetAppName(appImage));
        var promptResult = await userInteraction.PromptYesNo("Update AppImage", question, cancellationToken);
        if (!promptResult) return;
        
        var workingDirectory = programPaths.ProgramPath.Parent();
        var appImageToolPath = workingDirectory.GetFiles("AppImageUpdate-*.AppImage").FirstOrDefault();
        if (appImageToolPath != null)
        {
            await processes.RunProcess(
                appImageToolPath.FullPath(),
                [appImage.Path.FullPath()],
                cancellationToken);
        }
    }

    public async Task RemoveResources(AppImage appImage, DesktopResources desktopResources, CancellationToken cancellationToken = default)
    {
        var promptResult = await userInteraction.PromptYesNo(
            "Remove from Desktop?",
            string.Format(RemovalQuestion, GetAppName(appImage)),
            cancellationToken);
        if (promptResult)
            await inner.RemoveResources(appImage, desktopResources, cancellationToken);
    }

    private static string GetAppName(AppImage appImage) => appImage.Path.Basename;
}