using FluentAssertions;
using Machine.Specifications;
using NSubstitute;
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
					.PromptYesNo("Integrate zzVbM5PrSeWK into the Desktop?",
						Arg.Any<CancellationToken>())
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
					new AppImage { Path = new CompatPath(AppImageName) },
					new DesktopResources { DesktopEntry = desktopEntry, Icons = [] });
			};

			private It then_registers_the_desktop_image = () =>
				_registeredAppImage.Path.FullPath().Should().Be(AppImageName);

			private It then_registers_the_resources = () => _registeredDesktopResources.Should().Be(
				new DesktopResources { DesktopEntry = TestFixture.TestData / "Cura.desktop", Icons = [] });
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
					programPaths
						.GetProgramPathAsync(Arg.Any<CancellationToken>())
						.Returns(TestFixture.TestData / "fake-self");

					var userInteraction = Substitute.For<IUserInteraction>();
					userInteraction
						.PromptYesNo("Update rMZ2dJJdeb8?", Arg.Any<CancellationToken>())
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
						new AppImage { Path = new CompatPath(AppImageName) });
				};

				private It then_updates_using_the_appimagetool =
					() => _startedProgram.Should().EndWith("AppImageUpdate-archy.AppImage");

				private It then_updates_the_appimage = () => _updatedProgram.Should().EndWith(AppImageName);
			}
		}

		public class and_app_image_updater_is_not_available
		{
			public class when_updating_a_desktop_image
			{
				private const string AppImageName = "rMZ2dJJdeb8";

				private static string? _updatedProgram;

				private static readonly Lazy<InteractiveResourceManagement> DesktopAppRegistration = new(() =>
				{
					var inner = Substitute.For<IDesktopResourceManagement>();
					inner.UpdateImage(Arg.Any<AppImage>(), Arg.Any<CancellationToken>())
						.Returns(info =>
						{
							_updatedProgram = info.Arg<AppImage>().Path.FullPath();
							return Task.CompletedTask;
						});

					var processStarter = Substitute.For<IStartProcesses>();

					var programPaths = Substitute.For<IProgramPaths>();
					programPaths.GetProgramPathAsync(Arg.Any<CancellationToken>()).Returns(TestFixture.TestData);

					var userInteraction = Substitute.For<IUserInteraction>();
					userInteraction
						.PromptYesNo($"Update {AppImageName}?", Arg.Any<CancellationToken>())
						.Returns(true);

					userInteraction
						.DisplayIndeterminateProgress($"Updating {AppImageName}...", Arg.Any<CancellationToken>())
						.Returns(true);

					return new InteractiveResourceManagement(
						inner,
						userInteraction,
						programPaths,
						processStarter);
				});

				private Because of = async () =>
				{
					await DesktopAppRegistration.Value.UpdateImage(
						new AppImage { Path = new CompatPath(AppImageName) });
				};

				private It then_updates_using_the_inner_tool = () => _updatedProgram.Should().EndWith(AppImageName);
			}

			public class when_updating_a_desktop_image_and_the_user_cancels
			{
				private const string AppImageName = "wanda";

				private static string? _updatedProgram;

				private static bool _isUpdateCancelled;

				private static readonly Lazy<InteractiveResourceManagement> DesktopAppRegistration = new(() =>
				{
					var progressUpdateReady = new TaskCompletionSource();

					var inner = Substitute.For<IDesktopResourceManagement>();
					inner.UpdateImage(Arg.Any<AppImage>(), Arg.Any<CancellationToken>())
						.Returns(info =>
						{
							_updatedProgram = info.Arg<AppImage>().Path.FullPath();
							var ct = info.Arg<CancellationToken>();
							ct.Register(() => progressUpdateReady.SetResult());
							return Task.Run(async () =>
							{
								await progressUpdateReady.Task;
								_isUpdateCancelled = ct.IsCancellationRequested;
							});
						});

					var programPaths = Substitute.For<IProgramPaths>();
					programPaths.GetProgramPathAsync(Arg.Any<CancellationToken>()).Returns(TestFixture.TestData);

					var userInteraction = Substitute.For<IUserInteraction>();
					userInteraction
						.PromptYesNo($"Update {AppImageName}?", Arg.Any<CancellationToken>())
						.Returns(true);

					userInteraction
						.DisplayIndeterminateProgress($"Updating {AppImageName}...", Arg.Any<CancellationToken>())
						.Returns(false);

					return new InteractiveResourceManagement(
						inner,
						userInteraction,
						programPaths,
						Substitute.For<IStartProcesses>());
				});

				private Because of = async () =>
				{
					await DesktopAppRegistration.Value.UpdateImage(
						new AppImage { Path = new CompatPath(AppImageName) });
				};

				private It then_updates_using_the_inner_tool = () => _updatedProgram.Should().EndWith(AppImageName);

				private It then_handles_the_cancellation = () => _isUpdateCancelled.Should().BeTrue();
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
					programPaths
						.GetProgramPathAsync(Arg.Any<CancellationToken>())
						.Returns(TestFixture.TestData / "fake-self");

					var userInteraction = Substitute.For<IUserInteraction>();
					userInteraction
						.PromptYesNo(Arg.Any<string>(), Arg.Any<CancellationToken>())
						.Returns(true);
					userInteraction
						.PromptYesNo("Update GopalLian?", Arg.Any<CancellationToken>())
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
						new AppImage { Path = new CompatPath(AppImageName) });
				};

				private It then_does_not_update_the_appimage = () => _updatedProgram.Should().BeEmpty();
			}
		}

		public class and_the_resources_are_registered
		{
			public class when_removing_the_resources
			{
				private const string AppImageName = "xa6uxFIwo8e.AppImage";

				private static readonly AppImage _appImage = new() { Path = new CompatPath(AppImageName) };

				private static AppImage? _removedAppImage;
				private static DesktopResources? _removedDesktopResources;

				private static readonly Lazy<InteractiveResourceManagement> DesktopAppRegistration = new(() =>
				{
					var inner = Substitute.For<IDesktopResourceManagement>();
					inner
						.RemoveResources(
							Arg.Any<AppImage>(),
							Arg.Any<DesktopResources>(),
							Arg.Any<CancellationToken>())
						.Returns(callInfo =>
						{
							_removedAppImage = callInfo.Arg<AppImage>();
							_removedDesktopResources = callInfo.Arg<DesktopResources>();
							return Task.CompletedTask;
						});

					var userInteraction = Substitute.For<IUserInteraction>();
					userInteraction
						.PromptYesNo(
							"Remove xa6uxFIwo8e from Desktop?",
							Arg.Any<CancellationToken>())
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

					await DesktopAppRegistration.Value.RemoveResources(
						_appImage,
						new DesktopResources { DesktopEntry = desktopEntry, Icons = [], });
				};

				private It then_removes_the_desktop_image = () =>
					_removedAppImage.Path.FullPath().Should().Be(AppImageName);

				private It then_removes_the_resources = () => _removedDesktopResources.Should().Be(
					new DesktopResources { DesktopEntry = TestFixture.TestData / "Cura.desktop", Icons = [] });
			}
		}
	}
}
