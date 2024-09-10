namespace SharperAppImages.Registration;

public interface IDesktopAppRegistration
{
    Task RegisterResources(AppImage appImage, DesktopResources desktopResources);
}