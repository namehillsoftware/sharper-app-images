using FluentAssertions;
using Machine.Specifications;
using NSubstitute;
using PathLib;
using SharperIntegration.Access;
using SharperIntegration.Extraction;
using SharperIntegration.Verification;

namespace SharperIntegration.Test;

[Subject(nameof(FileSystemAppImageAccess))]
public class TestAppImageAccess
{
    public class Given_an_app_image_path
    {
        public class when_getting_the_executable_app_image
        {
            private static readonly Lazy<TempDirectory> tempDir = new();

            private static readonly Lazy<FileSystemAppImageAccess> sut = new(() =>
            {
                var config = Substitute.For<IAppImageExtractionConfiguration>();
                return new FileSystemAppImageAccess(config);
            });

            private static readonly Lazy<Task<IPath>> AppImagePath = new(async () =>
            {
                var path = TestFixture.TestData / "OUAeISsga.appimage";
                await path.Touch(existOk: true);
                return path;
            });

            private static AppImage? _appImage;

            private Because of = async () => _appImage = sut.Value.GetExecutableAppImage(await AppImagePath.Value);

            private It is_executable = () =>
                _appImage!.Path.FileInfo.UnixFileMode.Should().HaveFlag(UnixFileMode.UserExecute);

            private Cleanup after = () =>
            {
                if (tempDir.IsValueCreated) ((IDisposable)tempDir.Value).Dispose();
            };
        }
    }

    public class Given_an_executable_app_image
    {
        public class when_the_desktop_resources_are_requested
        {
            private static readonly Lazy<TempDirectory> tempDir = new();

            private static readonly Lazy<FileSystemAppImageAccess> sut = new(() =>
            {
                var config = Substitute.For<IAppImageExtractionConfiguration>();
                config.StagingDirectory.Returns(tempDir.Value);

                return new FileSystemAppImageAccess(config);
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

                private static readonly Lazy<FileSystemAppImageAccess> sut = new(() =>
                {
                    var config = Substitute.For<IAppImageExtractionConfiguration>();
                    config.StagingDirectory.Returns(tempDir.Value);

                    return new FileSystemAppImageAccess(config);
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

    public class Given_a_file_with_no_magical_bytes
    {
        public class And_without_an_extension
        {
            public class When_checking_if_the_file_is_an_app_image
            {
                private static readonly Lazy<FileSystemAppImageAccess> subject = new(() => new FileSystemAppImageAccess(Substitute.For<IAppImageExtractionConfiguration>()));

                private static bool isAppImage;

                private Because of = async () =>
                {
                    using var tempDirectory = new TempDirectory();
                    var testFile = tempDirectory / "cbMWgJdfoH";
                    await testFile.Touch();
                    isAppImage = await subject.Value.IsAppImage(testFile);
                };

                It is_not_an_app_image = () => isAppImage.Should().BeFalse();
            }
        }

        public class And_with_an_AppImage_extension
        {
            public class When_checking_if_the_file_is_an_app_image
            {
                private static readonly Lazy<AppImageChecker> subject = new();

                private static bool isAppImage;

                private Because of = async () =>
                {
                    using var tempDirectory = new TempDirectory();
                    var testFile = tempDirectory / "hQxOh7XZn2J.AppImage";
                    await testFile.Touch();
                    isAppImage = await subject.Value.IsAppImage(testFile);
                };

                It is_an_app_image = () => isAppImage.Should().BeTrue();
            }
        }

        public class And_with_an_appimage_extension
        {
            public class When_checking_if_the_file_is_an_app_image
            {
                private static readonly Lazy<FileSystemAppImageAccess> subject = new(() => new FileSystemAppImageAccess(Substitute.For<IAppImageExtractionConfiguration>()));

                private static bool isAppImage;

                private Because of = async () =>
                {
                    using var tempDirectory = new TempDirectory();
                    var testFile = tempDirectory / "kuPllU899P.appimage";
                    await testFile.Touch();
                    isAppImage = await subject.Value.IsAppImage(testFile);
                };

                It is_an_app_image = () => isAppImage.Should().BeTrue();
            }
        }
    }

    public class Given_a_file_with_magical_bytes
    {
        public class And_it_is_a_type_one_image
        {
            public class When_checking_if_the_file_is_an_app_image
            {
                private static readonly Lazy<FileSystemAppImageAccess> subject = new(() => new FileSystemAppImageAccess(Substitute.For<IAppImageExtractionConfiguration>()));

                private static bool isAppImage;

                private Because of = async () =>
                {
                    using var tempDir = new TempDirectory();
                    var testFile = tempDir / "test.magical";
                    await testFile.WriteAllBytesAsync([0, 0, 0, 0, 0, 0, 0, 0, 0x41, 0x49, 0x01]);
                    isAppImage = await subject.Value.IsAppImage(testFile);
                };

                It is_an_app_image = () => isAppImage.Should().BeTrue();
            }
        }

        public class And_it_is_a_type_two_image
        {
            public class When_checking_if_the_file_is_an_app_image
            {
                private static readonly Lazy<FileSystemAppImageAccess> subject = new(() => new FileSystemAppImageAccess(Substitute.For<IAppImageExtractionConfiguration>()));

                private static bool isAppImage;

                private Because of = async () =>
                {
                    using var tempDir = new TempDirectory();
                    var testFile = tempDir / "test.2magical";
                    await testFile.WriteAllBytesAsync([0, 0, 0, 0, 0, 0, 0, 0, 0x41, 0x49, 0x02]);
                    isAppImage = await subject.Value.IsAppImage(testFile);
                };

                It is_an_app_image = () => isAppImage.Should().BeTrue();
            }
        }

        public class And_it_is_an_unknown_type_image
        {
            public class When_checking_if_the_file_is_an_app_image
            {
                private static readonly Lazy<FileSystemAppImageAccess> subject = new(() => new FileSystemAppImageAccess(Substitute.For<IAppImageExtractionConfiguration>()));

                private static bool isAppImage;

                private Because of = async () =>
                {
                    using var tempDir = new TempDirectory();
                    var testFile = tempDir / "test.crazymagic";
                    await testFile.WriteAllBytesAsync([0, 0, 0, 0, 0, 0, 0, 0, 0x41, 0x49, 0xFA]);
                    isAppImage = await subject.Value.IsAppImage(testFile);
                };

                It is_an_app_image = () => isAppImage.Should().BeFalse();
            }
        }

        public class and_it_is_a_directory
        {
	        public class When_checking_if_the_file_is_an_app_image
	        {
		        private static readonly Lazy<FileSystemAppImageAccess> subject = new(() => new FileSystemAppImageAccess(Substitute.For<IAppImageExtractionConfiguration>()));

		        private static bool isAppImage;

		        private Because of = async () =>
		        {
			        using var tempDir = new TempDirectory();
			        isAppImage = await subject.Value.IsAppImage(tempDir);
		        };

		        It is_NOT_an_app_image = () => isAppImage.Should().BeFalse();
	        }
        }
    }
}
