using CliWrap;
using Microsoft.Extensions.Logging;

namespace SharperIntegration;

public class ProcessStarter(ILogger<ProcessStarter> logger) : IStartProcesses
{
    public async Task<int> RunProcess(string processName, string[] args, CancellationToken cancellationToken = default)
    {
        using (logger.BeginScope(processName))
        {
            var command = Cli.Wrap(processName)
                .WithArguments(args)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(line => logger.LogInformation(line)))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(line => logger.LogError(line)));
            var commandResult = await command.ExecuteAsync(cancellationToken);
            return commandResult.ExitCode;
        }
    }
}