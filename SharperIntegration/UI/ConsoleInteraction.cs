namespace SharperIntegration.UI;

public class ConsoleInteraction : IUserInteraction
{
    public async Task<bool> PromptYesNo(string title, string question, CancellationToken cancellationToken = default)
    {
        await Console.Out.WriteAsync($"{question} Press y to continue...");
        var result = Console.ReadKey();
        return result.Key == ConsoleKey.Y;
    }

    public Task<bool> DisplayIndeterminateProgress(string title, string information,
	    CancellationToken cancellationToken = default) => Task.FromResult(true);

    public async Task DisplayWarning(string title, string information, CancellationToken cancellationToken = default)
    {
	    await Console.Out.WriteAsync($"Warning! {information} Press any key to continue...");
	    Console.ReadKey();
    }
}
