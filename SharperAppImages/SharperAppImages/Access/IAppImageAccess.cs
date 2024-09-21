using PathLib;
using SharperAppImages.Extraction;
using SharperAppImages.Verification;

namespace SharperAppImages.Access;

public interface IAppImageAccess : IAppImageExtractor, ICheckAppImages
{
    AppImage GetExecutableAppImage(IPath appImagePath);
}