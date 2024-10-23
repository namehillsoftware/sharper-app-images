namespace SharperIntegration.UI;

public interface IUserInteraction
{
    Task<bool> PromptYesNo(string question, CancellationToken cancellationToken = default);

    Task<bool> DisplayIndeterminateProgress(string information, CancellationToken cancellationToken = default);

    Task DisplayWarning(string information, CancellationToken cancellationToken = default);
}
