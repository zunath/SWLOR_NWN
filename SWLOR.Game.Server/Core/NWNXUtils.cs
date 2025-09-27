using System.Runtime.InteropServices;

namespace SWLOR.Game.Server.Core
{
    internal class NWNXUtils
    {

        [DllImport("NWNX_Core", EntryPoint = "_ZN7NWNXLib5Utils13GetGameObjectEj", ExactSpelling = true)]
        public static extern nint GetGameObject(uint objectId);
    }
}
