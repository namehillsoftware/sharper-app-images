namespace SharperAppImages;

public interface IAppImageCheck
{
    Task<bool> IsAppImage(string path);
}