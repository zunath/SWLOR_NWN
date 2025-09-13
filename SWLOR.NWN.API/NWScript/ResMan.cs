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
            return NWN.Core.NWScript.ResManGetAliasFor(sResRef, (int)nResType);
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
            return NWN.Core.NWScript.ResManFindPrefix(sPrefix, (int)nResType, nNth, bSearchBaseData ? 1 : 0, sOnlyKeyTable);
        }

        /// <summary>
        /// Get the contents of a file as string, as seen by the server's resman.
        /// Note: If the file contains binary data it will return data up to the first null byte.
        /// - nResType: a RESTYPE_* constant.
        /// Returns "" if the file does not exist.
        /// </summary>
        public static string ResManGetFileContents(string sResRef, int nResType)
        {
            return NWN.Core.NWScript.ResManGetFileContents(sResRef, nResType);
        }

    }
}
