using FluentAssertions;
using Machine.Specifications;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PathLib;
using SharperAppImages;
using SharperAppImages.Extraction;

namespace Integration.Tests;

[Subject(nameof(AppImageExtractor))]
public class TestAppImageExtractor
{
    public class Given_an_executable_app_image
    {
        public class when_the_desktop_resources_are_requested
        {
            private static readonly Lazy<TempDirectory> tempDir = new();

            private static readonly Lazy<AppImageExtractor> sut = new(() =>
            {
                var config = Substitute.For<IAppImageExtractionConfiguration>();
                config.StagingDirectory.Returns(tempDir.Value);

                return new AppImageExtractor(Substitute.For<ILogger<AppImageExtractor>>(), config);
            });

            private static readonly Lazy<AppImage> ExecutableAppImage = new(() =>
            {
                var appImagePath = TestFixture.TestData / "Echo-x86_64.AppImage";
                appImagePath.FileInfo.UnixFileMode |= UnixFileMode.UserExecute;
                return new AppImage
                {
                    Path = appImagePath,
                };
            });

            private static DesktopResources? _desktopResources;

            private Because of = async () =>
                _desktopResources = await sut.Value.ExtractDesktopResources(ExecutableAppImage.Value);

            It has_the_correct_desktop_entry = () =>
                _desktopResources!.DesktopEntry!.FileInfo.FullName.Should().Match($"{tempDir}*echo.desktop");
            
            It has_the_correct_desktop_icon = () =>
                _desktopResources!.Icons.Single().FileInfo.FullName.Should().Match($"{tempDir}*utilities-terminal.svg");

            private Cleanup after = () =>
            {
                if (tempDir.IsValueCreated) ((IDisposable)tempDir.Value).Dispose();
            };
        }
        
        public class and_it_does_not_execute
        {
            public class when_the_desktop_resources_are_requested
            {
                private static readonly Lazy<TempDirectory> tempDir = new();

                private static readonly Lazy<AppImageExtractor> sut = new(() =>
                {
                    var config = Substitute.For<IAppImageExtractionConfiguration>();
                    config.StagingDirectory.Returns(tempDir.Value);

                    return new AppImageExtractor(Substitute.For<ILogger<AppImageExtractor>>(), config);
                });

                private static readonly Lazy<AppImage> ExecutableAppImage = new(() =>
                {
                    var appImagePath = TestFixture.TestData / "AppImageExtract_6-x86_64.AppImage";
                    appImagePath.FileInfo.UnixFileMode |= UnixFileMode.UserExecute;
                    return new AppImage
                    {
                        Path = appImagePath,
                    };
                });

                private static UnexpectedAppImageExecutionCode? _unexpectedExtractionCodeException;
                private static DesktopResources? _desktopResources;

                private Because of = async () =>
                {
                    try
                    {
                        _desktopResources = await sut.Value.ExtractDesktopResources(ExecutableAppImage.Value);
                    }
                    catch (UnexpectedAppImageExecutionCode e)
                    {
                        _unexpectedExtractionCodeException = e;
                    }
                };

                It has_the_correct_exception = () => _unexpectedExtractionCodeException.Should().NotBeNull();

                private Cleanup after = () =>
                {
                    if (tempDir.IsValueCreated) ((IDisposable)tempDir.Value).Dispose();
                };
            }
        }
    }
}