using PathLib;
using SharperIntegration.Extraction;
using SharperIntegration.Verification;

namespace SharperIntegration.Access;

public interface IAppImageAccess : IAppImageExtractor, ICheckAppImages
{
    AppImage GetExecutableAppImage(IPath appImagePath);
}