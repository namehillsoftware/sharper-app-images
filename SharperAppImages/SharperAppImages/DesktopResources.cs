using PathLib;

namespace SharperAppImages;

public record DesktopResources
{
    public IPath? DesktopEntry  { get; init; } = null;
    public IEnumerable<IPath> Icons { get; init; } = [];
};