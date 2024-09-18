using FluentAssertions;
using Integration.Tests;
using Machine.Specifications;
using NSubstitute;
using SharperAppImages.Extraction;

namespace SharperAppImages.Test;

[Subject(nameof(FileSystemAppImageExtractor))]
public class TestAppImageExtractor
{
    public class Given_an_executable_app_image
    {
        public class when_the_desktop_resources_are_requested
        {
            private static readonly Lazy<TempDirectory> tempDir = new();

            private static readonly Lazy<FileSystemAppImageExtractor> sut = new(() =>
            {
                var config = Substitute.For<IAppImageExtractionConfiguration>();
                config.StagingDirectory.Returns(tempDir.Value);

                return new FileSystemAppImageExtractor(config);
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

                private static readonly Lazy<FileSystemAppImageExtractor> sut = new(() =>
                {
                    var config = Substitute.For<IAppImageExtractionConfiguration>();
                    config.StagingDirectory.Returns(tempDir.Value);

                    return new FileSystemAppImageExtractor(config);
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

                private static InvalidOperationException _invalidOperationException;

                private Because of = async () =>
                {
                    try
                    {
                        await sut.Value.ExtractDesktopResources(ExecutableAppImage.Value);
                    }
                    catch (InvalidOperationException e)
                    {
                        _invalidOperationException = e;
                    }
                };

                It has_the_correct_exception = () => _invalidOperationException.Message.Should().Be("No RockRidge file information available");

                private Cleanup after = () =>
                {
                    if (tempDir.IsValueCreated) ((IDisposable)tempDir.Value).Dispose();
                };
            }
        }
    }
}