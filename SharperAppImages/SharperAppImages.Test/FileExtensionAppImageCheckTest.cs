using FluentAssertions;

namespace SharperAppImages.Test;

public class FileExtensionAppImageCheckTest
{
    [Fact]
    public async Task TestFileNameWithoutExtension()
    {
        var fileExtensionAppImageCheck = new AppImageChecker();
        using var testFile = new UsableTestFile();
        (await fileExtensionAppImageCheck.IsAppImage(testFile.FilePath)).Should().BeFalse();
    }
    
    [Fact]
    public async Task TestFileNameWithAppImageExtension()
    {
        var fileExtensionAppImageCheck = new AppImageChecker();
        using var testFile = new UsableTestFile(".AppImage");
        (await fileExtensionAppImageCheck.IsAppImage(testFile.FilePath)).Should().BeTrue();
    }
    
    [Fact]
    public async Task TestFileNameWith_appimage_Extension()
    {
        var fileExtensionAppImageCheck = new AppImageChecker();
        using var testFile = new UsableTestFile(".appimage");
        (await fileExtensionAppImageCheck.IsAppImage(testFile.FilePath)).Should().BeTrue();
    }

    [Fact]
    public async Task TestMagicalTypeOneAppImage()
    {
        var appImageCheck = new AppImageChecker();

        using var testFile = new UsableTestFile();
        await File.WriteAllBytesAsync(testFile.FilePath, [0, 0, 0, 0, 0, 0, 0, 0, 0x41, 0x49, 0x01]);
        var isAppImage = await appImageCheck.IsAppImage(testFile.FilePath);
        isAppImage.Should().BeTrue();
    }

    private class UsableTestFile(string extension = "") : IDisposable
    {
        private readonly Lazy<string> _lazyFilePath = new(() =>
        {
            var path = Path.GetTempFileName() + extension;
            File.Create(path).Dispose();
            return path;
        });
        
        public string FilePath => _lazyFilePath.Value;

        public void Dispose()
        {
            if (_lazyFilePath.IsValueCreated) File.Delete(_lazyFilePath.Value);
        }
    }
}