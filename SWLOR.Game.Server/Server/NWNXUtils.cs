using System.Runtime.InteropServices;

namespace SWLOR.Shared.Core.Server
{
    internal class NWNXUtils
    {

        [DllImport("NWNX_Core", EntryPoint = "_ZN7NWNXLib5Utils13GetGameObjectEj", ExactSpelling = true)]
        public static extern nint GetGameObject(uint objectId);
    }
}
