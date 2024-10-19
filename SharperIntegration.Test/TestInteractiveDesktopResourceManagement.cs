using FluentAssertions;
using Machine.Specifications;
using NSubstitute;
using PathLib;
using SharperIntegration.Extraction;
using SharperIntegration.Registration;
using SharperIntegration.UI;

namespace SharperIntegration.Test;

[Subject(nameof(InteractiveResourceManagement))]
public class TestInteractiveDesktopResourceManagement
{
    public class Given_an_AppImage
    {
        public class when_registering_a_desktop_image
        {
            private const string AppImageName = "zzVbM5PrSeWK.appimage";
        
            private static AppImage? _registeredAppImage;
            private static DesktopResources? _registeredDesktopResources;
            
            private static readonly Lazy<InteractiveResourceManagement> DesktopAppRegistration = new(() =>
            {
                var inner = Substitute.For<IDesktopResourceManagement>();
                inner
                    .RegisterResources(
                        Arg.Any<AppImage>(),
                        Arg.Any<DesktopResources>(),
                        Arg.Any<CancellationToken>())
                    .Returns(callInfo =>
                    {
                        _registeredAppImage = callInfo.Arg<AppImage>();
                        _registeredDesktopResources = callInfo.Arg<DesktopResources>();
                        return Task.CompletedTask;
                    });
                
                var userInteraction = Substitute.For<IUserInteraction>();
                userInteraction
                    .PromptYesNo("Integrate into Desktop?", "Integrate zzVbM5PrSeWK into the Desktop?", Arg.Any<CancellationToken>())
                    .Returns(true);
                
                return new InteractiveResourceManagement(
                    inner,
                    userInteraction,
                    Substitute.For<IProgramPaths>(),
                    Substitute.For<IStartProcesses>());
            });

            private Because of = async () =>
            {
                var desktopEntry = TestFixture.TestData / "Cura.desktop";
                
                await DesktopAppRegistration.Value.RegisterResources(
                    new AppImage
                    {
                        Path = new CompatPath(AppImageName)
                    },
                    new DesktopResources
                    {
                        DesktopEntry = desktopEntry,
                        Icons = []
                    });
            };

            private It then_registers_the_desktop_image = () =>
                _registeredAppImage.Path.FullPath().Should().Be(AppImageName);

            private It then_registers_the_resources = () => _registeredDesktopResources.Should().Be(
                new DesktopResources
                {
                    DesktopEntry = TestFixture.TestData / "Cura.desktop",
                    Icons = []
                });
        }

        public class and_app_image_updater_is_available
        {
            public class when_updating_a_desktop_image
            {
                private const string AppImageName = "rMZ2dJJdeb8";

                private static string? _startedProgram;
                private static string? _updatedProgram;

                private static readonly Lazy<InteractiveResourceManagement> DesktopAppRegistration = new(() =>
                {
                    var processStarter = Substitute.For<IStartProcesses>();
                    processStarter
                        .RunProcess(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                        .Returns(info =>
                        {
                            _startedProgram = info.Arg<string>();
                            _updatedProgram = info.Arg<string[]>()[0];
                            return 0;
                        });

                    var programPaths = Substitute.For<IProgramPaths>();
                    programPaths.ProgramPath.Returns(TestFixture.TestData / "fake-self");
                    
                    var userInteraction = Substitute.For<IUserInteraction>();
                    userInteraction
                        .PromptYesNo("Update AppImage", "Update rMZ2dJJdeb8?", Arg.Any<CancellationToken>())
                        .Returns(true);

                    return new InteractiveResourceManagement(
                        Substitute.For<IDesktopResourceManagement>(),
                        userInteraction,
                        programPaths,
                        processStarter);
                });

                private Because of = async () =>
                {
                    await DesktopAppRegistration.Value.UpdateImage(
                        new AppImage
                        {
                            Path = new CompatPath(AppImageName)
                        });
                };

                private It then_updates_using_the_appimagetool =
                    () => _startedProgram.Should().EndWith("AppImageUpdate-archy.AppImage");

                private It then_updates_the_appimage = () => _updatedProgram.Should().EndWith(AppImageName);
            }
        }
        
         public class and_the_user_chooses_to_not_update
        {
            public class when_updating_a_desktop_image
            {
                private const string AppImageName = "GopalLian";

                private static string _updatedProgram = string.Empty;

                private static readonly Lazy<InteractiveResourceManagement> DesktopAppRegistration = new(() =>
                {
                    var processStarter = Substitute.For<IStartProcesses>();
                    processStarter
                        .RunProcess(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                        .Returns(info =>
                        {
                            _updatedProgram = info.Arg<string[]>()[0];
                            return 0;
                        });

                    var programPaths = Substitute.For<IProgramPaths>();
                    programPaths.ProgramPath.Returns(TestFixture.TestData / "fake-self");
                    
                    var userInteraction = Substitute.For<IUserInteraction>();
                    userInteraction
                        .PromptYesNo(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                        .Returns(true);
                    userInteraction
                        .PromptYesNo("Update AppImage", "Update GopalLian?", Arg.Any<CancellationToken>())
                        .Returns(false);

                    return new InteractiveResourceManagement(
                        Substitute.For<IDesktopResourceManagement>(),
                        userInteraction,
                        programPaths,
                        processStarter);
                });

                private Because of = async () =>
                {
                    await DesktopAppRegistration.Value.UpdateImage(
                        new AppImage
                        {
                            Path = new CompatPath(AppImageName)
                        });
                };

                private It then_does_not_update_the_appimage = () => _updatedProgram.Should().BeEmpty();
            }
        }

        public class and_the_resources_are_registered
        {
            public class when_removing_the_resources
            {
                private const string AppImageName = "xa6uxFIwo8e.AppImage";
                private const string MimeTypesEntry = "xa6uxFIwo8e.AppImage.desktop";

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

                private static readonly Lazy<IPath> MimeConfFile = new(() => MimeConfigDir.Value / "conf" / "mimetypes.conf");
                
                private static readonly Lazy<IPath> StagingDir = new(() => new TempDirectory());

                private static readonly Lazy<Task<DesktopResourceManagement>> DesktopAppRegistration = new(async () =>
                {
                    var extractionConfig = Substitute.For<IAppImageExtractionConfiguration>();
                    extractionConfig.StagingDirectory.Returns(StagingDir.Value);

                    var desktopAppLocations = Substitute.For<IDesktopAppLocations>();
                    desktopAppLocations.DesktopEntryDirectory.Returns(await LauncherDir.Value);
                    desktopAppLocations.IconDirectory.Returns(await IconsDir.Value);
                    desktopAppLocations.MimeConfigPath.Returns(MimeConfFile.Value);

                    return new DesktopResourceManagement(
                        extractionConfig,
                        desktopAppLocations,
                        Substitute.For<IProgramPaths>(),
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

                    var file = MimeConfFile.Value;
                    file.Parent().Mkdir(makeParents: true);
                    await file.WriteText($"""
                                          [Default Applications]
                                          application/octet-stream=xed.desktop
                                          image/png={MimeTypesEntry}
                                          
                                          [Added Associations]
                                          application/octet-stream=xed.desktop;{MimeTypesEntry};ff.desktop
                                          application/vnd.appimage=Sharper_Integration-x86_64.AppImage.desktop;
                                          image/png={MimeTypesEntry}
                                          """);
                    
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

                private It unregisters_the_mime_types = async () =>
                    MimeConfFile.Value.ReadAsText().Trim()
                        .Should()
                        .BeEquivalentTo("""
                                        [Default Applications]
                                        application/octet-stream=xed.desktop
                                        image/png=
                                        
                                        [Added Associations]
                                        application/octet-stream=xed.desktop;ff.desktop
                                        application/vnd.appimage=Sharper_Integration-x86_64.AppImage.desktop
                                        image/png=
                                        """);
                
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