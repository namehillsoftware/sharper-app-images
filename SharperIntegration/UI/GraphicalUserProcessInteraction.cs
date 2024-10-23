namespace SharperIntegration.UI;

public abstract class GraphicalUserProcessInteraction(IStartProcesses processes) : IUserInteraction
{
	private const string AppName = "Sharper Integration";

    public async Task<bool> PromptYesNo(string question, CancellationToken cancellationToken = default)
    {
        var (dialogCommand, arguments) = GetYesNoDialogCommand(AppName, question);
        if (!string.IsNullOrEmpty(dialogCommand))
        {
            return await processes.RunProcess(dialogCommand, arguments, cancellationToken) == 0;
        }

        return false;
    }

    public async Task<bool> DisplayIndeterminateProgress(string information, CancellationToken cancellationToken = default)
    {
	    var (dialogCommand, arguments) = GetIndeterminateProgressCommand(AppName, information);
	    if (!string.IsNullOrEmpty(dialogCommand))
	    {
		    return await processes.RunProcess(dialogCommand, arguments, cancellationToken) == 0;
	    }

	    return false;
    }

    public async Task DisplayWarning(string information, CancellationToken cancellationToken = default)
    {
	    var (dialogCommand, arguments) = GetWarningDialogCommand(AppName, information);
	    if (!string.IsNullOrEmpty(dialogCommand))
	    {
		    await processes.RunProcess(dialogCommand, arguments, cancellationToken);
	    }
    }

    protected abstract (string, string[]) GetYesNoDialogCommand(string title, string question);

    protected abstract (string, string[]) GetIndeterminateProgressCommand(string title, string information);

    protected abstract (string, string[]) GetWarningDialogCommand(string title, string information);
}
