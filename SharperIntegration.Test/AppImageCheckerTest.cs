using FluentAssertions;
using Machine.Specifications;
using SharperIntegration.Verification;

namespace SharperIntegration.Test;

[Subject(nameof(AppImageChecker))]
public class AppImageCheckerTest
{
    public class Given_a_file_with_no_magical_bytes
    {
        public class And_without_an_extension
        {
            public class When_checking_if_the_file_is_an_app_image
            {
                private static readonly Lazy<AppImageChecker> subject = new();

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
                private static readonly Lazy<AppImageChecker> subject = new();

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
                private static readonly Lazy<AppImageChecker> subject = new();

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
                private static readonly Lazy<AppImageChecker> subject = new();

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
                private static readonly Lazy<AppImageChecker> subject = new();

                private static bool isAppImage;

                private Because of = async () =>
                {
                    using var tempDir = new TempDirectory();
                    var testFile = tempDir / "test.crazymagic";
                    await testFile.WriteAllBytesAsync([0, 0, 0, 0, 0, 0, 0, 0, 0x41, 0x49, 0xFA]);
                    isAppImage = await subject.Value.IsAppImage(testFile);
                };

                It is_NOT_an_app_image = () => isAppImage.Should().BeFalse();
            }
        }
    }
}
