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

        /// <summary>
        /// Get the contents of a file as string, as seen by the server's resman.
        /// Note: If the output contains binary data it will only return data up to the first null byte.
        /// - nResType: a RESTYPE_* constant.
        /// - nFormat: one of RESMAN_FILE_CONTENTS_FORMAT_*
        /// Returns "" if the file does not exist.
        /// </summary>
        public static string ResManGetFileContents(string sResRef, int nResType, ResmanFileContentsFormatType nFormat = ResmanFileContentsFormatType.Raw)
        {
            VM.StackPush((int)nFormat);
            VM.StackPush(nResType);
            VM.StackPush(sResRef);
            VM.Call(1071);

            return VM.StackPopString();
        }

    }
}
