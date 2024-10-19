namespace SharperIntegration.UI;

public abstract class GraphicalUserProcessInteraction(IStartProcesses processes) : IUserInteraction
{
    public async Task<bool> PromptYesNo(string title, string question, CancellationToken cancellationToken = default)
    {
        var (dialogCommand, arguments) = GetYesNoDialogCommand(title, question);
        if (!string.IsNullOrEmpty(dialogCommand))
        {
            return await processes.RunProcess(dialogCommand, arguments, cancellationToken) == 0;
        }

        return false;
    }

    public async Task<bool> DisplayIndeterminateProgress(string title, string information, CancellationToken cancellationToken = default)
    {
	    var (dialogCommand, arguments) = GetIndeterminateProgressCommand(title, information);
	    if (!string.IsNullOrEmpty(dialogCommand))
	    {
		    return await processes.RunProcess(dialogCommand, arguments, cancellationToken) == 0;
	    }

	    return false;
    }

    protected abstract (string, string[]) GetYesNoDialogCommand(string title, string question);

    protected abstract (string, string[]) GetIndeterminateProgressCommand(string title, string information);
}
