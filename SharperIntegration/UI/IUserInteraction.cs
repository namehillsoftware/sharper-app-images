namespace SharperIntegration.UI;

public interface IUserInteraction
{
    Task<bool> PromptYesNo(string title, string question, CancellationToken cancellationToken = default);
}