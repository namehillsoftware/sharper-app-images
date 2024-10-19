namespace SharperIntegration.UI;

public class ZenityInteraction(IStartProcesses processes) : GraphicalUserProcessInteraction(processes)
{
    protected override (string, string[]) GetYesNoDialogCommand(string title, string question) =>
	    ("zenity",
	    [
		    "--question",
		    $"--title={title}",
		    $"--text={question}",
	    ]);

    protected override (string, string[]) GetIndeterminateProgressCommand(string title, string information) =>
	    ("zenity",
	    [
		    "--progress",
		    "--pulsate",
		    $"--title={title}",
		    $"--text={information}",
	    ]);
}
