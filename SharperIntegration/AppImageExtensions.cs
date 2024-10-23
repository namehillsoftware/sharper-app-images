namespace SharperIntegration;

public static class AppImageExtensions
{
	public static string GetAppName(this AppImage appImage) => appImage.Path.Basename;
}
