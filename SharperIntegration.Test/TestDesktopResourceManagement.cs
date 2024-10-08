using FluentAssertions;
using Machine.Specifications;
using NSubstitute;
using PathLib;
using SharperIntegration.Extraction;
using SharperIntegration.Registration;

namespace SharperIntegration.Test;

[Subject(nameof(DesktopResourceManagement))]
public class TestDesktopResourceManagement
{
    public class Given_an_AppImage
    {
        public class when_registering_a_desktop_image
        {
            private const string AppImageName = "zVzvC;ArxmK.appimage";
            
            private static readonly Lazy<IPath> IconsDir = new(() => new TempDirectory());
            private static readonly Lazy<IPath> LauncherDir = new(() => new TempDirectory());
            private static readonly Lazy<IPath> StagingDir = new(() => new TempDirectory());
            private static readonly Lazy<IPath> MimeConfigDir = new(() => new TempDirectory());

            private static readonly Lazy<IPath> MimeConfFile = new(() => MimeConfigDir.Value / "conf" / "mimetypes.conf");

            private static readonly Lazy<string> ExpectedAppImageFileName =
                new(() => new CompatPath(AppImageName).FileInfo.FullName);
            
            private static readonly Lazy<Task<DesktopResourceManagement>> DesktopAppRegistration = new(async () =>
            {
                var extractionConfig = Substitute.For<IAppImageExtractionConfiguration>();
                extractionConfig.StagingDirectory.Returns(StagingDir.Value);
                
                var desktopAppLocations = Substitute.For<IDesktopAppLocations>();
                desktopAppLocations.DesktopEntryDirectory.Returns(LauncherDir.Value);
                desktopAppLocations.IconDirectory.Returns(IconsDir.Value);

                var mimeConfigFile = MimeConfFile.Value;
                mimeConfigFile.Parent().Mkdir(makeParents: true);
                await mimeConfigFile.WriteText($"""
                                                [Default Applications]
                                                application/octet-stream=xed.desktop

                                                [Added Associations]
                                                application/octet-stream=xed.desktop;{AppImageName.Replace(";", "\\;")};ff.desktop
                                                application/vnd.appimage=Sharper_Integration-x86_64.AppImage.desktop;
                                                """);
                desktopAppLocations.MimeConfigPath.Returns(mimeConfigFile);
                
                return new DesktopResourceManagement(
                    extractionConfig,
                    desktopAppLocations,
                    Substitute.For<IStartProcesses>());
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
                
                await (await DesktopAppRegistration.Value).RegisterResources(
                    new AppImage
                    {
                        Path = new CompatPath(AppImageName)
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
            
            private It then_has_the_correct_launcher = () => (LauncherDir.Value / "zVzvC;ArxmK.appimage.desktop").ReadAsText()
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
                                Exec={ExpectedAppImageFileName.Value} %F
                                TryExec={ExpectedAppImageFileName.Value}
                                Icon=cura-icon
                                Terminal=false
                                Type=Application
                                MimeType=application/sla;application/vnd.ms-3mfdocument;application/prs.wavefront-obj;image/bmp;image/gif;image/jpeg;image/png;model/x3d+xml;application/octet-stream;
                                Categories=Graphics;
                                Keywords=3D;Printing;
                                Actions=remove-app-image;new-window;new-private-window
                                
                                [Desktop Action new-window]
                                Name=Open a New Window
                                Exec={ExpectedAppImageFileName.Value} %u
                                
                                [Desktop Action new-private-window]
                                Name=Open a New Private Window
                                Exec={ExpectedAppImageFileName.Value} --private-window %u
                                
                                [Desktop Action profilemanager]
                                Name=Open the Profile Manager
                                Exec={ExpectedAppImageFileName.Value} --ProfileManager %u
                                
                                [Desktop Action remove-app-image]
                                Name=Remove AppImage from Desktop
                                Exec={Environment.CommandLine} {ExpectedAppImageFileName.Value} --remove
                                """);

            private It then_installs_icons = () => IconsDir.Value.GetFiles("*", SearchOption.AllDirectories)
                .Select(f => f.FileInfo.FullName)
                .Should()
                .BeEquivalentTo(
                    (IconsDir.Value / "scalable" / "apps" / "icon.svg").ToString(),
                    (IconsDir.Value / "scalable" / "mimetypes" / "app-x.svg").ToString(),
                    (IconsDir.Value / "big-dumb-icon.png").ToString());

            private It then_registers_the_mime_types = () => MimeConfFile.Value
                .ReadAsText()
                .Trim()
                .Should()
                .BeEquivalentTo($"""
                                [Default Applications]
                                application/octet-stream=xed.desktop
                                
                                [Added Associations]
                                application/octet-stream=xed.desktop;{AppImageName.Replace(";", "\\;")};ff.desktop
                                application/vnd.appimage=Sharper_Integration-x86_64.AppImage.desktop;
                                application/sla={AppImageName.Replace(";", "\\;")}
                                application/vnd.ms-3mfdocument={AppImageName.Replace(";", "\\;")}
                                application/prs.wavefront-obj={AppImageName.Replace(";", "\\;")}
                                image/bmp={AppImageName.Replace(";", "\\;")}
                                image/gif={AppImageName.Replace(";", "\\;")}
                                image/jpeg={AppImageName.Replace(";", "\\;")}
                                image/png={AppImageName.Replace(";", "\\;")}
                                model/x3d+xml={AppImageName.Replace(";", "\\;")}
                                """);

            private Cleanup after = () =>
            {
                if (IconsDir.IsValueCreated) ((IDisposable)IconsDir.Value).Dispose();
                if (LauncherDir.IsValueCreated) ((IDisposable)LauncherDir.Value).Dispose();
                if (StagingDir.IsValueCreated) ((IDisposable)StagingDir.Value).Dispose();
                if (MimeConfigDir.IsValueCreated) ((IDisposable)MimeConfigDir.Value).Dispose();
            };
        }
        
        public class when_registering_a_different_desktop_image
        {
            private static readonly Lazy<IPath> IconsDir = new(() => new TempDirectory());
            private static readonly Lazy<IPath> LauncherDir = new(() => new TempDirectory());
            private static readonly Lazy<IPath> StagingDir = new(() => new TempDirectory());

            private static readonly Lazy<string> ExpectedAppImagePath =
                new(() => new CompatPath("fuMkJ3PJS.AppImage").FileInfo.FullName);
            
            private static readonly Lazy<DesktopResourceManagement> DesktopAppRegistration = new(() =>
            {
                var extractionConfig = Substitute.For<IAppImageExtractionConfiguration>();
                extractionConfig.StagingDirectory.Returns(StagingDir.Value);
                
                var desktopAppLocations = Substitute.For<IDesktopAppLocations>();
                desktopAppLocations.DesktopEntryDirectory.Returns(LauncherDir.Value);
                desktopAppLocations.IconDirectory.Returns(IconsDir.Value);
                
                return new DesktopResourceManagement(
                    extractionConfig, 
                    desktopAppLocations,
                    Substitute.For<IStartProcesses>());
            });

            private Because of = async () =>
            {
                var desktopEntry = TestFixture.TestData / "TweakedCura.desktop";
                
                var iconsDir = StagingDir.Value / "usr/share/icons";
                var nested  = iconsDir / "Cursuselit";

                var scalableIcon  = nested / "apps" / "icon.svgz";
                scalableIcon.Parent().Mkdir(makeParents: true);
                await scalableIcon.Touch();

                var mimeType = nested / "mimetypes" / "app-x.svg";
                mimeType.Parent().Mkdir(makeParents: true);
                await mimeType.Touch();
                
                await DesktopAppRegistration.Value.RegisterResources(
                    new AppImage
                    {
                        Path = new CompatPath("fuMkJ3PJS.AppImage")
                    },
                    new DesktopResources
                    {
                        DesktopEntry = desktopEntry,
                        Icons = 
                        [
                            scalableIcon,
                            mimeType,
                        ]
                    });
            };
            
            private It then_has_the_correct_launcher = () => (LauncherDir.Value / "fuMkJ3PJS.AppImage.desktop").ReadAsText()
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
                                Exec={ExpectedAppImagePath.Value} %F
                                TryExec={ExpectedAppImagePath.Value}
                                Icon=cura-icon
                                Terminal=false
                                Type=Application
                                MimeType=application/sla;application/vnd.ms-3mfdocument;application/prs.wavefront-obj;image/bmp;image/gif;image/jpeg;image/png;model/x3d+xml;
                                Categories=Graphics;
                                Keywords=3D;Printing;
                                Actions=remove-app-image;Atligula;Euismodsuspendisse
                                
                                [Desktop Action Atligula]
                                Name=Open a New Window
                                Exec={ExpectedAppImagePath.Value} %u
                                
                                [Desktop Action Euismodsuspendisse]
                                Name=Open a New Private Window
                                Exec={ExpectedAppImagePath.Value} --private-window %u
                                
                                [Desktop Action profilemanager]
                                Name=Open the Profile Manager
                                Exec={ExpectedAppImagePath.Value} --ProfileManager %u
                                
                                [Desktop Action remove-app-image]
                                Name=Remove AppImage from Desktop
                                Exec={Environment.CommandLine} {ExpectedAppImagePath.Value} --remove
                                """);

            private It then_installs_icons = () => IconsDir.Value.GetFiles("*", SearchOption.AllDirectories)
                .Select(f => f.FileInfo.FullName)
                .Should()
                .BeEquivalentTo(
                    (IconsDir.Value / "Cursuselit" / "apps" / "icon.svgz").ToString(),
                    (IconsDir.Value / "Cursuselit" / "mimetypes" / "app-x.svg").ToString());

            private Cleanup after = () =>
            {
                if (IconsDir.IsValueCreated) ((IDisposable)IconsDir.Value).Dispose();
                if (LauncherDir.IsValueCreated) ((IDisposable)LauncherDir.Value).Dispose();
                if (StagingDir.IsValueCreated) ((IDisposable)StagingDir.Value).Dispose();
            };
        }

        public class and_the_resources_are_registered
        {
            public class when_removing_the_resources
            {
                private const string AppImageName = "xa6uxFIwo8e.AppImage";
                
                private static readonly AppImage _appImage = new()
                {
                    Path = new CompatPath(AppImageName)
                };
                
                private static readonly Lazy<IPurePath[]> RelativeIconPaths = new(() =>
                {
                    var scalableDir = PurePath.Create("scalable");
                    
                    return
                    [
                        scalableDir.Join("apps", "JrUje2K23.jpg"),
                        scalableDir.Join("mimetypes", "ztSWdcaCH.svg"),
                        PurePath.Create("smart-little-icon.png"),
                    ];
                });
                
                private static readonly Lazy<Task<IPath>> IconsDir = new(async () =>
                {
                    var iconsDir = new TempDirectory();

                    foreach (var iconPath in RelativeIconPaths.Value)
                    {
                        var path = new CompatPath(iconsDir / iconPath);
                        path.Parent().Mkdir(true);

                        await path.Touch(true);
                    }
                    
                    return iconsDir;
                });
                
                private static readonly Lazy<Task<IPath>> LauncherDir = new(async () =>
                {
                    var tempDir = new TempDirectory();
                    
                    var installedLauncher = tempDir / $"{_appImage.Path.Filename}.desktop";
                    await installedLauncher.Touch(true);
                    return tempDir;
                });
                
                private static readonly Lazy<IPath> MimeConfigDir = new(() => new TempDirectory());

                private static readonly Lazy<Task<IPath>> MimeConfFile = new(async () =>
                {
                    var file = MimeConfigDir.Value / "conf" / "mimetypes.conf";
                    file.Parent().Mkdir(makeParents: true);
                    await file.WriteText($"""
                                          [Default Applications]
                                          application/octet-stream=xed.desktop

                                          [Added Associations]
                                          application/octet-stream=xed.desktop;{AppImageName};ff.desktop
                                          application/vnd.appimage=Sharper_Integration-x86_64.AppImage.desktop;
                                          """);
                    return file;
                });
                
                private static readonly Lazy<IPath> StagingDir = new(() => new TempDirectory());

                private static readonly Lazy<Task<DesktopResourceManagement>> DesktopAppRegistration = new(async () =>
                {
                    var extractionConfig = Substitute.For<IAppImageExtractionConfiguration>();
                    extractionConfig.StagingDirectory.Returns(StagingDir.Value);

                    var desktopAppLocations = Substitute.For<IDesktopAppLocations>();
                    desktopAppLocations.DesktopEntryDirectory.Returns(await LauncherDir.Value);
                    desktopAppLocations.IconDirectory.Returns(await IconsDir.Value);

                    return new DesktopResourceManagement(
                        extractionConfig,
                        desktopAppLocations,
                        Substitute.For<IStartProcesses>());
                });

                private Because of = async () =>
                {
                    var desktopEntry = TestFixture.TestData / "Cura.desktop";

                    var iconsDir = new CompatPath("usr/share/icons");

                    var desktopIcons = new List<IPath>();
                    
                    foreach (var iconPath in RelativeIconPaths.Value)
                    {
                        var path = new CompatPath(iconsDir / iconPath);
                        path.Parent().Mkdir(true);

                        await path.Touch(existOk: true);
                        desktopIcons.Add(path);
                    }
                    
                    await (await DesktopAppRegistration.Value).RemoveResources(
                        _appImage,
                        new DesktopResources
                        {
                            DesktopEntry = desktopEntry,
                            Icons = desktopIcons,
                        });
                };

                private It has_an_empty_launcher_desktop_dir = async () =>
                    (await LauncherDir.Value).GetFiles("*").Should().BeEmpty();
                
                private It has_an_empty_icons_dir = async () =>
                    (await IconsDir.Value).GetFiles("*").Should().BeEmpty();
                
                private Cleanup after = () =>
                {
                    if (IconsDir.IsValueCreated) ((IDisposable)IconsDir.Value).Dispose();
                    if (LauncherDir.IsValueCreated) ((IDisposable)LauncherDir.Value).Dispose();
                    if (StagingDir.IsValueCreated) ((IDisposable)StagingDir.Value).Dispose();
                };
            }
        }
    }
}