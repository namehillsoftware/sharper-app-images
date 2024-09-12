using FluentAssertions;
using Integration.Tests;
using Machine.Specifications;
using NSubstitute;
using PathLib;
using SharperAppImages.Extraction;
using SharperAppImages.Registration;

namespace SharperAppImages.Test;

[Subject(nameof(DesktopAppRegistration))]
public class TestDesktopRegistration
{
    public class Given_an_AppImage
    {
        public class when_registering_a_desktop_image
        {
            private static readonly Lazy<IPath> IconsDir = new(() => new TempDirectory());
            private static readonly Lazy<IPath> LauncherDir = new(() => new TempDirectory());
            private static readonly Lazy<IPath> StagingDir = new(() => new TempDirectory());
            
            private static readonly Lazy<DesktopAppRegistration> DesktopAppRegistration = new(() =>
            {
                var extractionConfig = Substitute.For<IAppImageExtractionConfiguration>();
                extractionConfig.StagingDirectory.Returns(StagingDir.Value);
                
                var desktopAppLocations = Substitute.For<IDesktopAppLocations>();
                desktopAppLocations.DesktopEntryDirectory.Returns(LauncherDir.Value);
                desktopAppLocations.IconDirectory.Returns(IconsDir.Value);
                
                return new DesktopAppRegistration(extractionConfig, desktopAppLocations);
            });

            private Because of = async () =>
            {
                var desktopEntry = TestFixture.TestData / "Cura.desktop";
                
                var iconsDir = StagingDir.Value / "usr/share/icons";
                var scalable  = iconsDir / "scalable";

                var scalableIcon  = iconsDir / "scalable" / "apps" / "icon.svg";
                scalableIcon.Parent().Mkdir(makeParents: true);
                await scalableIcon.Touch();

                var mimeType = scalable / "mimetypes" / "app-x.svg";
                mimeType.Parent().Mkdir(makeParents: true);
                await mimeType.Touch();

                var rootIcon = StagingDir.Value / "big-dumb-icon.png";
                await rootIcon.Touch();
                
                await DesktopAppRegistration.Value.RegisterResources(
                    new AppImage
                    {
                        Path = new CompatPath("zVzvCArxmK.appimage")
                    },
                    new DesktopResources
                    {
                        DesktopEntry = desktopEntry,
                        Icons = [
                            scalableIcon,
                            mimeType,
                            rootIcon
                        ]
                    });
            };
            
            private It then_has_the_correct_launcher = () => (LauncherDir.Value / "Cura.desktop").ReadAsText()
                .Trim()
                .Should()
                .BeEquivalentTo($"""
                                [Desktop Entry]
                                Name=Ultimaker Cura
                                Name[de]=Ultimaker Cura
                                GenericName=3D Printing Software
                                GenericName[de]=3D-Druck-Software
                                Comment=Cura converts 3D models into paths for a 3D printer. It prepares your print for maximum accuracy, minimum printing time and good reliability with many extra features that make your print come out great.
                                Comment[de]=Cura wandelt 3D-Modelle in Pfade für einen 3D-Drucker um. Es bereitet Ihren Druck für maximale Genauigkeit, minimale Druckzeit und guter Zuverlässigkeit mit vielen zusätzlichen Funktionen vor, damit Ihr Druck großartig wird.
                                Exec={new CompatPath("zVzvCArxmK.appimage").FileInfo.FullName} %F
                                TryExec=cura
                                Icon=cura-icon
                                Terminal=false
                                Type=Application
                                MimeType=application/sla;application/vnd.ms-3mfdocument;application/prs.wavefront-obj;image/bmp;image/gif;image/jpeg;image/png;model/x3d+xml;
                                Categories=Graphics;
                                Keywords=3D;Printing;
                                """);

            private It then_installs_icons = () => IconsDir.Value.GetFiles("*", SearchOption.AllDirectories)
                .Select(f => f.FileInfo.FullName)
                .Should()
                .BeEquivalentTo(
                    (IconsDir.Value / "scalable" / "apps" / "icon.svg").ToString(),
                    (IconsDir.Value / "scalable" / "mimetypes" / "app-x.svg").ToString(),
                    (IconsDir.Value / "big-dumb-icon.png").ToString());

            private Cleanup after = () =>
            {
                if (IconsDir.IsValueCreated) ((IDisposable)IconsDir.Value).Dispose();
                if (LauncherDir.IsValueCreated) ((IDisposable)LauncherDir.Value).Dispose();
                if (StagingDir.IsValueCreated) ((IDisposable)StagingDir.Value).Dispose();
            };
        }
    }
}