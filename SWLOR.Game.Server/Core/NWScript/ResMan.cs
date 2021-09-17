using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {

        /// <summary>
        /// Returns the resource location of sResRef.nResType, as seen by the running module.
        /// Note for dedicated servers: Checks on the module/server side, not the client.
        /// Returns "" if the resource does not exist in the search space.
        /// </summary>
        public static string ResManGetAliasFor(string sResRef, ResType nResType)
        {
            Internal.NativeFunctions.StackPushInteger((int)nResType);
            Internal.NativeFunctions.StackPushString(sResRef);
            Internal.NativeFunctions.CallBuiltIn(1008);

            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        /// Finds the nNth available resref starting with sPrefix.
        /// * Set bSearchBaseData to TRUE to also search base game content stored in your game installation directory.
        ///   WARNING: This can be very slow.
        /// * Set sOnlyKeyTable to a specific keytable to only search the given named keytable (e.g. "OVERRIDE:").
        /// Returns "" if no such resref exists.
        /// </summary>
        public static string ResManFindPrefix(string sPrefix, ResType nResType, int nNth = 1, bool bSearchBaseData = false, string sOnlyKeyTable = "")
        {
            Internal.NativeFunctions.StackPushString(sOnlyKeyTable);
            Internal.NativeFunctions.StackPushInteger(bSearchBaseData ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger(nNth);
            Internal.NativeFunctions.StackPushInteger((int)nResType);
            Internal.NativeFunctions.StackPushString(sPrefix);
            Internal.NativeFunctions.CallBuiltIn(1008);

            return Internal.NativeFunctions.StackPopString();
        }

    }
}
