namespace SharperIntegration.UI;

public interface IDialogControl
{
    Task<string[]> GetYesNoDialogCommand(string title, string question, CancellationToken cancellationToken = default);
}