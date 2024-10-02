using PathLib;

namespace SharperIntegration.Extraction;

public interface IAppImageExtractionConfiguration
{
    IPath StagingDirectory { get; }
}