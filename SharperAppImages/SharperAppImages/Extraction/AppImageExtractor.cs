using System.Diagnostics;
using PathLib;

namespace SharperAppImages.Extraction;

public class AppImageExtractor(IAppImageExtractionConfiguration extractionConfiguration) : IAppImageExtractor
{
    public async Task<DesktopResources?> ExtractDesktopResources(AppImage appImage)
    {
        var desktopEntryTask = GetDesktopEntry(appImage);
        var desktopIconTask = GetDesktopIcon(appImage);

        return new DesktopResources
        {
            DesktopEntry = await desktopEntryTask,
            Icons = await desktopIconTask,
        };
    }
    
    private async Task<IPath?> GetDesktopEntry(AppImage appImage)
    {
        var resources = await GetResources(appImage, "desktop");

        return resources.FirstOrDefault();
    }

    private async Task<IEnumerable<IPath>> GetDesktopIcon(AppImage appImage)
    {
        var resourceResults = await Task.WhenAll(
            GetResources(appImage, "png"),
            GetResources(appImage, "svg"),
            GetResources(appImage, "svgz"),
            GetResources(appImage, "jpg"),
            GetResources(appImage, "jpeg")
        );

        return resourceResults.SelectMany(resource => resource).Distinct();
    }

    private async Task<IEnumerable<IPath>> GetResources(AppImage appImage, string resourceExtension)
    {
        var searchPattern = $"*.{resourceExtension}";
        
        var stagingDirectory = extractionConfiguration.StagingDirectory;
        
        var process = Process.Start(new ProcessStartInfo(appImage.Path.FileInfo.FullName, ["--appimage-extract", searchPattern])
        {
            WorkingDirectory = stagingDirectory.DirectoryInfo.FullName,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        });
        
        if (process == null) return [];
        
        await process.WaitForExitAsync();

        if (process.ExitCode == 0) return stagingDirectory.GetFiles(searchPattern, SearchOption.AllDirectories);
        
        var errorOutputTask = process.StandardError.ReadToEndAsync();
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        throw new UnexpectedAppImageExecutionCode(
            process.ExitCode,
            await errorOutputTask,
            await standardOutputTask);
    }
}