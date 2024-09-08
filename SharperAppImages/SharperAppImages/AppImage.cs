using PathLib;

namespace SharperAppImages;

public record AppImage
{
    public IPath Path { get; init; } = new CompatPath("");
}