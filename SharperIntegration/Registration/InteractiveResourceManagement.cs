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
    private const string UpdateInProgress = "Updating {0}...";

    public async Task RegisterResources(AppImage appImage, DesktopResources desktopResources, CancellationToken cancellationToken = default)
    {
        var question = string.Format(RegistrationQuestion, appImage.GetAppName());
        var promptResult = await userInteraction.PromptYesNo(question, cancellationToken);
        if (promptResult)
            await inner.RegisterResources(appImage, desktopResources, cancellationToken);
    }

    public async Task UpdateImage(AppImage appImage, CancellationToken cancellationToken = default)
    {
	    var appName = appImage.GetAppName();
	    var question = string.Format(UpdateQuestion, appName);
        var promptResult = await userInteraction.PromptYesNo(question, cancellationToken);
        if (!promptResult) return;

        var programPath = await programPaths.GetProgramPathAsync(cancellationToken);
        var workingDirectory = programPath.Parent();
        var appImageToolPath = workingDirectory.GetFiles("AppImageUpdate-*.AppImage").FirstOrDefault();
        if (appImageToolPath != null)
        {
            await processes.RunProcess(
                appImageToolPath.FullPath(),
                [appImage.Path.FullPath()],
                cancellationToken);
            return;
        }

        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var progressDisplay = userInteraction.DisplayIndeterminateProgress(
	        string.Format(UpdateInProgress, appName),
	        linkedCancellationTokenSource.Token);
        var updateTask = inner.UpdateImage(appImage, linkedCancellationTokenSource.Token);

        await Task.WhenAny(progressDisplay, updateTask);
        linkedCancellationTokenSource.Cancel();

        await Task.WhenAll(progressDisplay, updateTask);
    }

    public async Task RemoveResources(AppImage appImage, DesktopResources desktopResources, CancellationToken cancellationToken = default)
    {
	    var question = string.Format(RemovalQuestion, appImage.GetAppName());
        var promptResult = await userInteraction.PromptYesNo(question, cancellationToken);
        if (promptResult)
            await inner.RemoveResources(appImage, desktopResources, cancellationToken);
    }
}
