using PathLib;

namespace SharperAppImages.Verification;

public class AppImageChecker : ICheckAppImages
{
    public async Task<bool> IsAppImage(IPath path)
    {
        await using var appImageSteam = path.Open(FileMode.Open);
        var newPosition = appImageSteam.Seek(8, SeekOrigin.Begin);
        if (newPosition < 0) return IsAppImagePath(path);

        var magicBytes = new byte[3];
        if (await appImageSteam.ReadAsync(magicBytes) != 3) return IsAppImagePath(path);

        if (magicBytes[0] == 0x41 && magicBytes[1] == 0x49)
        {
            var thirdByte = magicBytes[2];
            return thirdByte is 0x01 or 0x02 || IsAppImagePath(path);
        }
    
        return IsAppImagePath(path);
    }

    private static bool IsAppImagePath(IPath path) => path.Extension is ".AppImage" or ".appimage";
}