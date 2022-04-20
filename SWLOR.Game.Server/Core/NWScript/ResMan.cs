﻿using SWLOR.Game.Server.Core.NWScript.Enum;

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
            VM.StackPush((int)nResType);
            VM.StackPush(sResRef);
            VM.Call(1008);

            return VM.StackPopString();
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
            VM.StackPush(sOnlyKeyTable);
            VM.StackPush(bSearchBaseData ? 1 : 0);
            VM.StackPush(nNth);
            VM.StackPush((int)nResType);
            VM.StackPush(sPrefix);
            VM.Call(1009);

            return VM.StackPopString();
        }

    }
}
