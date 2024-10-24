using PathLib;

namespace SharperIntegration.Registration;

public interface IProgramPaths
{
	public Task<IPath> GetProgramPathAsync(CancellationToken cancellationToken = default);
}
