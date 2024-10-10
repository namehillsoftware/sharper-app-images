using System.Diagnostics;

namespace SharperIntegration;

public class ProcessStarter : IStartProcesses
{
    public async Task<int> RunProcess(string processName, string[] args, CancellationToken cancellationToken = default)
    {
        var process = Process.Start(processName, args);
        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }
}