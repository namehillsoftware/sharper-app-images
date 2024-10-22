namespace SharperIntegration.UI;

public interface IUserInteraction
{
    Task<bool> PromptYesNo(string title, string question, CancellationToken cancellationToken = default);

    Task<bool> DisplayIndeterminateProgress(string title, string information, CancellationToken cancellationToken = default);

    Task DisplayWarning(string title, string information, CancellationToken cancellationToken = default);
}
