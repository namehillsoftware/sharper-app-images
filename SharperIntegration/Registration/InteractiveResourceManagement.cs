using SharperIntegration.UI;

namespace SharperIntegration.Registration;

public class InteractiveResourceManagement(IDesktopResourceManagement inner, IUserInteraction userInteraction) : IDesktopResourceManagement
{
    private const string RegistrationQuestion = "Integrate {0} into the Desktop?";
    private const string RemovalQuestion = "Remove {0} from Desktop?";
    
    public async Task RegisterResources(AppImage appImage, DesktopResources desktopResources, CancellationToken cancellationToken = default)
    {
        var promptResult = await userInteraction.PromptYesNo(
            "Integrate into Desktop?",
            string.Format(RegistrationQuestion, GetAppName(appImage)),
            cancellationToken);
        if (promptResult)
            await inner.RegisterResources(appImage, desktopResources, cancellationToken);
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