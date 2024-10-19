namespace SharperIntegration.UI;

public class KDialogInteraction(IStartProcesses processes) : GraphicalUserProcessInteraction(processes)
{
    protected override (string, string[]) GetYesNoDialogCommand(string title, string question) =>
	    ("kdialog",
	    [
		    $"--title={title}",
		    $"--yesno={question}",
	    ]);

    protected override (string, string[]) GetIndeterminateProgressCommand(string title, string information) =>
	    ("kdialog",
	    [
		    $"--msgbox={title}{Environment.NewLine}{Environment.NewLine}{information}",
	    ]);
}
