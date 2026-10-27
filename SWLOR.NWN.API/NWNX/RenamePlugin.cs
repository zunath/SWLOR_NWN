using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX
{
    public static class RenamePlugin
    {
        public static void SetPCNameOverride(
            uint target,
            string newName,
            string prefix = "",
            string suffix = "",
            PlayerNameOverrideType type = PlayerNameOverrideType.Default,
            uint observer = OBJECT_INVALID)
        {
            RenamePluginRuntime.Current.SetPCNameOverride(target, newName, prefix, suffix, type, observer);
        }

        public static string GetPCNameOverride(uint target, uint observer = OBJECT_INVALID)
        {
            return RenamePluginRuntime.Current.GetPCNameOverride(target, observer);
        }

        public static void ClearPCNameOverride(uint target, uint observer = OBJECT_INVALID, bool clearAll = false)
        {
            RenamePluginRuntime.Current.ClearPCNameOverride(target, observer, clearAll);
        }
    }
}
