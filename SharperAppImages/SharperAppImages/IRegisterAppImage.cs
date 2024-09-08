using PathLib;

namespace SharperAppImages;

public interface IRegisterAppImage
{
    Task RegisterAppImage(IPath path);
}