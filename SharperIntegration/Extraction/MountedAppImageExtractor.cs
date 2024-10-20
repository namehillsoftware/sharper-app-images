using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using PathLib;

namespace SharperIntegration.Extraction;

public class MountedAppImageExtractor(
    ILogger<MountedAppImageExtractor> logger,
    IAppImageExtractionConfiguration extractionConfiguration) : IAppImageExtractor
{
    public async Task<DesktopResources?> ExtractDesktopResources(AppImage appImage, CancellationToken cancellationToken = default)
    {
        var (mountProcess, mountedImagePath) = await GetMountedImage(appImage, cancellationToken);

        if (mountProcess == null) return null;

        using (mountProcess)
        {
            try
            {
                if (mountedImagePath == null)
                {
                    using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    linkedCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));
                    await mountProcess.WaitForExitAsync(linkedCancellationTokenSource.Token);

                    var exitCode = mountProcess.ExitCode;
                    if (exitCode != 0)
                    {
                        throw new UnexpectedAppImageExecutionCode(
                            mountProcess.ExitCode,
                            await mountProcess.StandardOutput.ReadToEndAsync(cancellationToken),
                            await mountProcess.StandardError.ReadToEndAsync(cancellationToken));
                    }

                    return null;
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        mountedImagePath.GetFiles("*");
                        break;
                    }
                    catch (IOException e)
                    {
                        logger.LogWarning(e, "Mount path {mountPath} does not yet exist.", mountedImagePath);
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    }
                }

                var desktopEntries = GetResources(mountedImagePath, "desktop", cancellationToken);

                var resources = new DesktopResources
                {
                    DesktopEntry = desktopEntries.FirstOrDefault(),
                    Icons = GetDesktopIcons(mountedImagePath, cancellationToken).ToArray(),
                };

                return resources;
            }
            finally
            {
                mountProcess.Kill(true);

                await mountProcess.WaitForExitAsync(cancellationToken);
            }
        }
    }

    private IEnumerable<IPath> GetDesktopIcons(IPath mountPath, CancellationToken cancellationToken = default)
    {
        IEnumerable<IPath> resources =
        [
            ..GetResources(mountPath, "png", cancellationToken),
            ..GetResources(mountPath, "svg", cancellationToken),
            ..GetResources(mountPath, "svgz", cancellationToken),
            ..GetResources(mountPath, "jpg", cancellationToken),
            ..GetResources(mountPath, "jpeg", cancellationToken),
        ];

        return resources.Distinct();
    }

    private static async Task<(Process?, IPath?)> GetMountedImage(AppImage appImage,
        CancellationToken cancellationToken = default)
    {
        var process = Process.Start(new ProcessStartInfo(appImage.Path.FileInfo.FullName, "--appimage-mount")
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = Encoding.UTF8,
        });

        if (process == null) return (null, null);

        var mountProcess = Process.Start(
            new ProcessStartInfo("mount")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });

        var mountOutput = await (mountProcess?.StandardOutput.ReadToEndAsync(cancellationToken) ?? Task.FromResult(""));
        await (mountProcess?.WaitForExitAsync(cancellationToken) ?? Task.CompletedTask);
        var mountStrings = mountOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var mountPath = mountStrings
            .Where(s => s.StartsWith(appImage.Path.Filename))
            .Select(s =>
            {
                var tokens = s.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                return tokens.Length > 2 ? new CompatPath(tokens[2]) : null;
            }).LastOrDefault();

        return (process, mountPath);
    }

    private List<IPath> GetResources(IPath mountPath, string resourceExtension, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

        var searchPattern = $"*.{resourceExtension}";
        var tmpFiles = mountPath.GetFiles(searchPattern, SearchOption.AllDirectories).ToArray();
        var resources = new List<IPath>(tmpFiles.Length);
        var stagingDirectory = extractionConfiguration.StagingDirectory;
        foreach (var tmpFile in tmpFiles)
        {
            if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

            if (tmpFile.IsSymlink()) continue;

            var relativePath = tmpFile.RelativeTo(mountPath);
            var stagingPath = stagingDirectory / relativePath;
            stagingPath.Parent().Mkdir(makeParents: true);
            tmpFile.CopyTo(stagingPath);
            resources.Add(stagingPath);
        }

        return resources;
    }
}
