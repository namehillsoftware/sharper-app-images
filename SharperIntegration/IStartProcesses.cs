using System.Diagnostics;

namespace SharperIntegration;

public interface IStartProcesses
{
    Task RunProcess(string processName, string[] args, CancellationToken cancellationToken = default);
}