namespace SharperAppImages;

public class AppImageChecker : IAppImageCheck
{
    public async Task<bool> IsAppImage(string path)
    {
        await using var appImageSteam = File.OpenRead(path);
        var newPosition = appImageSteam.Seek(8, SeekOrigin.Begin);
        if (newPosition < 0) return IsAppImagePath(path);

        var magicBytes = new byte[3];
        if (await appImageSteam.ReadAsync(magicBytes.AsMemory(0, 3)) != 3) return IsAppImagePath(path);

        if (magicBytes[0] == 0x41 && magicBytes[1] == 0x49)
        {
            var thirdByte = magicBytes[2];
            return thirdByte is 0x01 or 0x02 || IsAppImagePath(path);
        }
    
        return IsAppImagePath(path);
    }
    
    private static bool IsAppImagePath(string path)
    {
        var extension = Path.GetExtension(path);
        return extension is ".AppImage" or ".appimage";
    }
}