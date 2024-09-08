using PathLib;

namespace SharperAppImages.Verification;

public class DelegatingAppImageCheck(ICheckAppImages inner) : ICheckAppImages
{
    public virtual Task<bool> IsAppImage(IPath path) => inner.IsAppImage(path);
}