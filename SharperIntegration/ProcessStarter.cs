using System.Diagnostics;

namespace SharperIntegration;

public class ProcessStarter : IStartProcesses
{
    public Task RunProcess(string processName, string[] args, CancellationToken cancellationToken = default) =>
        Process.Start(processName, args).WaitForExitAsync(cancellationToken);
}