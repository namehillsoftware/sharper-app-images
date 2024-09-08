using System.Security.Cryptography;
using FluentAssertions;
using Machine.Specifications;
using NSubstitute;
using PathLib;

namespace Integration.Tests.Integrator;

[Subject("Desktop Integrator")]
public class TestDesktopIntegrator
{
  
    [Fact]
    public async Task GivenAnAppImage_ThenFilesIntegrateIntoTheCorrectDirectory()
    {
        using var homeDir = new TempDirectory();

        var applicationPaths = Substitute.For<IApplicationPaths>();
        applicationPaths.XdgHomeDirectory.Returns(homeDir);
        
        var desktopIntegration = new DesktopIntegrator(applicationPaths);

        var appImagePath = TestFixture.TestData / "Echo-x86_64.AppImage";
        await using var imagePathStream = appImagePath.Open(FileMode.Open);
        
        await desktopIntegration.Integrate(new CompatPath(appImagePath));
        
        using var md5 = MD5.Create();
        var hash = md5.ComputeHashAsync(appImagePath.Open(FileMode.Open));

        (homeDir / $"applications/appimagekit_{hash}-Echo.desktop").Exists().Should().BeTrue();
        
        (homeDir / $"icons/hicolor/scalable/apps/appimagekit_{hash}_utilities-terminal.svg")
            .Exists()
            .Should()
            .BeTrue();
    }
}

public class DesktopIntegrator(IApplicationPaths applicationPaths)
{
    public async Task Integrate(IPath appImagePath, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

public interface IApplicationPaths
{
    IPath XdgHomeDirectory { get; }
}