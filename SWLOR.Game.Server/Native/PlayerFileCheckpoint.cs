using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Persists the migration checkpoint and the craft file sequence in BIC exports, which omit creature locals. The craft file
    /// sequence (<c>CraftFileSeq</c>) is how a login tells whether the character file already holds an item transfer the player
    /// record says happened (see <c>CraftSessionLifecycle</c>); it is written only once it is above zero, so files of characters
    /// that never crafted carry no extra field.
    /// </summary>
    public static class PlayerFileCheckpoint
    {
        internal const string FieldName = "PlayerFileVer";
        internal const string CraftSequenceFieldName = "CraftFileSeq";

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            NativeHookAccess.Hooks.Enable(NativeHookId.PlayerFileCheckpoint);
        }
    }
}
