using SharperAppImages.Extraction;

namespace SharperAppImages.Registration;

public class DesktopAppRegistration(
    IAppImageExtractionConfiguration appImageExtractionConfiguration, 
    IDesktopAppLocations appLocations
) : IDesktopAppRegistration
{
    public async Task RegisterResources(AppImage appImage, DesktopResources desktopResources)
    {
        var stagedUsrShare = appImageExtractionConfiguration.StagingDirectory / "usr" / "share";
        foreach (var icon in desktopResources.Icons)
        {
            var newIconPath = icon.RelativeTo(stagedUsrShare);
            newIconPath = appLocations.IconDirectory / newIconPath;
            newIconPath.Parent().Mkdir(makeParents: true);
            icon.FileInfo.CopyTo(newIconPath.FileInfo.FullName, true);
        }
    }
}