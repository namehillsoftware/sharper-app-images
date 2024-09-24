using PathLib;

namespace SharperIntegration;

public record AppImage
{
    public IPath Path { get; init; } = new CompatPath("");
}