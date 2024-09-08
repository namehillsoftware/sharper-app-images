# AppImageLauncher Pros

- Seamlessly integrates AppImages on launch of an AppImage.
- The desktop entry has a sane format and name.

## Cons

- Some apps can't run?!
- Some apps seem to crash!?
- Doesn't seem to follow appimageupdate conventions.

# GoAppImage Pros

- Integrates as an appimage.
- Runs as a user space service.

## Cons

- Apps are automatically removed for seemingly no reason.

# This Project

- As a user, I want to be able to right-click on a program and integrate it into my desktop environment, so that I can launch it via normal means.

## Requirements

- Correct Icon must be extracted and used.
- Correct desktop file must be extracted and used, desktop file should remain the same.
- Desktop file should have additional to update (if AppImageUpdate is on path).
