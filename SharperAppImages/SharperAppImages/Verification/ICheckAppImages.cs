using PathLib;

namespace SharperAppImages.Verification;

public interface ICheckAppImages
{
    Task<bool> IsAppImage(IPath path);
}