using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {

        /// <summary>
        /// Returns the resource location of the specified resource, as seen by the running module.
        /// Note for dedicated servers: Checks on the module/server side, not the client.
        /// </summary>
        /// <param name="sResRef">The resource reference</param>
        /// <param name="nResType">The resource type</param>
        /// <returns>The resource location, or empty string if the resource does not exist in the search space</returns>
        public static string ResManGetAliasFor(string sResRef, ResType nResType)
        {
            return global::NWN.Core.NWScript.ResManGetAliasFor(sResRef, (int)nResType);
        }

        /// <summary>
        /// Finds the nth available resref starting with the specified prefix.
        /// Set bSearchBaseData to TRUE to also search base game content stored in your game installation directory.
        /// WARNING: This can be very slow.
        /// Set sOnlyKeyTable to a specific keytable to only search the given named keytable (e.g. "OVERRIDE:").
        /// </summary>
        /// <param name="sPrefix">The prefix to search for</param>
        /// <param name="nResType">The resource type</param>
        /// <param name="nNth">The nth occurrence to find (defaults to 1)</param>
        /// <param name="bSearchBaseData">Whether to also search base game content (WARNING: This can be very slow)</param>
        /// <param name="sOnlyKeyTable">Specific keytable to search (e.g. "OVERRIDE:")</param>
        /// <returns>The found resref, or empty string if no such resref exists</returns>
        public static string ResManFindPrefix(string sPrefix, ResType nResType, int nNth = 1, bool bSearchBaseData = false, string sOnlyKeyTable = "")
        {
            return global::NWN.Core.NWScript.ResManFindPrefix(sPrefix, (int)nResType, nNth, bSearchBaseData ? 1 : 0, sOnlyKeyTable);
        }

        /// <summary>
        /// Gets the contents of a file as string, as seen by the server's resman.
        /// Note: If the file contains binary data it will return data up to the first null byte.
        /// </summary>
        /// <param name="sResRef">The resource reference</param>
        /// <param name="nResType">A RESTYPE_* constant</param>
        /// <returns>The file contents, or empty string if the file does not exist</returns>
        public static string ResManGetFileContents(string sResRef, int nResType)
        {
            return global::NWN.Core.NWScript.ResManGetFileContents(sResRef, nResType);
        }

    }
}
