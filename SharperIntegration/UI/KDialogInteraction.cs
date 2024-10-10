namespace SharperIntegration.UI;

public class KDialogInteraction(IStartProcesses processes) : GraphicalUserProcessInteraction(processes)
{
    protected override (string, string[]) GetYesNoDialogCommand(string title, string question)
    {
        return ("kdialog",
        [
            $"--title={title}",
            $"--yesno={question}",
        ]);
    }
}