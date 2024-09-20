using PathLib;
using SharperAppImages.Verification;

namespace SharperAppImages.Extraction;

public interface IAppImageAccess : IAppImageExtractor, ICheckAppImages
{
    AppImage GetExecutableAppImage(IPath appImagePath);
}