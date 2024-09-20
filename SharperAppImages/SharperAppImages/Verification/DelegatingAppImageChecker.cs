using PathLib;

namespace SharperAppImages.Verification;

public class DelegatingAppImageChecker(ICheckAppImages inner) : ICheckAppImages
{
    public virtual Task<bool> IsAppImage(IPath path, CancellationToken cancellationToken = default) => 
        inner.IsAppImage(path, cancellationToken);
}