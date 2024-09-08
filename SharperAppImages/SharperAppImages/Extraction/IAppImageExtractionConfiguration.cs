using PathLib;

namespace SharperAppImages.Extraction;

public interface IAppImageExtractionConfiguration
{
    IPath StagingDirectory { get; }
}