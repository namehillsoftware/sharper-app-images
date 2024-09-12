namespace SharperAppImages.Extraction;

public interface IAppImageExtractor
{
    Task<DesktopResources?> ExtractDesktopResources(AppImage appImage, CancellationToken cancellationToken = default);
}