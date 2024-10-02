using PathLib;

namespace SharperIntegration.Verification;

public interface ICheckAppImages
{
    Task<bool> IsAppImage(IPath path, CancellationToken cancellationToken = default);
}