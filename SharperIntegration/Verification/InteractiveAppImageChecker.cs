using PathLib;
using SharperIntegration.UI;

namespace SharperIntegration.Verification;

public class InteractiveAppImageChecker(IUserInteraction userInteraction, ICheckAppImages inner) : ICheckAppImages
{
	public async Task<bool> IsAppImage(IPath path, CancellationToken cancellationToken = default)
	{
		var isAppImage = await inner.IsAppImage(path, cancellationToken);
		if (!isAppImage)
			await userInteraction.DisplayWarning($"{path.Filename} is not an app image.", cancellationToken);
		return isAppImage;
	}
}
