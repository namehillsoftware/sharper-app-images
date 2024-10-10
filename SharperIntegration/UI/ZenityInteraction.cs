namespace SharperIntegration.UI;

public class ZenityInteraction(IStartProcesses processes) : GraphicalUserProcessInteraction(processes)
{
    protected override (string, string[]) GetYesNoDialogCommand(string title, string question)
    {
        return ("zenity",
        [
            "--question",
            $"--title={title}",
            $"--text={question}",
        ]);
    }
}