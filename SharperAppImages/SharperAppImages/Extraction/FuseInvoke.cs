using System.Runtime.InteropServices;

namespace SharperAppImages.Extraction;

public class FuseInvoke
{
    [DllImport("squashfuse")]
    static extern unsafe int fusefs_main(int argc, string[] argv, void* mountedCallback);
}