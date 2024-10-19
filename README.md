# Sharper Integration

<img src="./AppImage/sharper-integration.svg" width="300" alt="Sharper Integration Logo" title="Sharper Integration Logo" />

Sharper Integration is an AppImage desktop integration program primarily written in C#.

Its features include:

- Extracts and integrates the desktop icon and desktop file from the AppImage and integrates them with the desktop.
- Registers App Images with their associated mime-types.
- Upon installation of itself, associates itself with the AppImage mime-type.
- Adds options to undo desktop integration and to update the AppImage from the menu (using AppImageUpdate or appimageupdatetool if present in the same directory as SharperIntegration).

## Try it out

Download SharperIntegration from [here](https://github.com/namehillsoftware/sharper-app-images/releases) and [make it executable](https://discourse.appimage.org/t/how-to-make-an-appimage-executable/).

## Motivation

- As a user, I want to be able to right-click on a program and integrate it into my desktop environment, so that I can launch it via my normal desktop conventions.

## Requirements

- Correct Icon must be extracted and used.
- Correct desktop file must be extracted and used, desktop file should remain the same.
- Integration must be reversible.
- Desktop file should have the option to update (if AppImageUpdate or appimageupdatetool are on the path).
  - If AppImageUpdate is on the path, then that should be launched, otherwise appimageupdatetool should be used if it is on the path.

## Development Resources

- [XDG Desktop Entry Specification](https://xdg.pages.freedesktop.org/xdg-specs/desktop-entry-spec/latest/index.html#introduction)
- [AppImageKit](https://github.com/AppImage/AppImageKit)
- [Awesome AppImage](https://github.com/AppImageCommunity/awesome-appimage?tab=readme-ov-file)
- Numerous wonderful C# packages:
  - [SharperIntegration](./SharperIntegration/SharperIntegration.csproj)
  - [SharperIntegration.Test](./SharperIntegration.Test/SharperIntegration.Test.csproj)

# Alternatives

## AppImageLauncher

###  Pros

- Seamlessly integrates AppImages on launch of an AppImage.
- The desktop entry has a sane format and name.

### Cons

- Some apps can't run?!
- Some apps seem to crash!?
- Doesn't seem to follow appimageupdate conventions.

## GoAppImage

### Pros

- Integrates as an appimage.
- Runs as a user space service.

### Cons

- Apps are automatically removed for seemingly no reason (usually related to the PATH changing).
