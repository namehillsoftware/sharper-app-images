namespace SharperAppImages;

public class DelegatingAppImageCheck(IAppImageCheck inner) : IAppImageCheck
{
    public virtual Task<bool> IsAppImage(string path) => inner.IsAppImage(path);
}