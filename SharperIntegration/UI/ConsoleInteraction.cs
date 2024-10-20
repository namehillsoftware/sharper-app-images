namespace SharperIntegration.UI;

public class ConsoleInteraction : IUserInteraction
{
    public Task<bool> PromptYesNo(string title, string question, CancellationToken cancellationToken = default)
    {
        Console.Write($"{question} Press y to continue...");
        var result = Console.ReadKey();
        return Task.FromResult(result.Key == ConsoleKey.Y);
    }

    public Task<bool> DisplayIndeterminateProgress(string title, string information,
	    CancellationToken cancellationToken = default) => Task.FromResult(true);
}
