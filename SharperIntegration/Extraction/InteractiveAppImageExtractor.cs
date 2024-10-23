using SharperIntegration.UI;

namespace SharperIntegration.Extraction;

public class InteractiveAppImageExtractor(IUserInteraction userInteraction, IAppImageExtractor inner) : IAppImageExtractor
{
	public async Task<DesktopResources?> ExtractDesktopResources(AppImage appImage, CancellationToken cancellationToken = default)
	{
		var resources = await inner.ExtractDesktopResources(appImage, cancellationToken);
		if (resources == null)
			await userInteraction.DisplayWarning(
				$"No desktop resources found for {appImage.GetAppName()}.", cancellationToken);

		return resources;
	}
}
