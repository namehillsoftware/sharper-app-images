using System.Diagnostics;

namespace SharperIntegration;

public interface IStartProcesses
{
    Task<int> RunProcess(string processName, string[] args, CancellationToken cancellationToken = default);
}