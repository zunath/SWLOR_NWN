using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Crafting.Minigame;

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
        internal const string FieldName = PlayerFileFieldNames.FileVersion;
        internal const string CraftSequenceFieldName = PlayerFileFieldNames.CraftSequence;

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHooks()
        {
            NativeHookAccess.Hooks.Enable(NativeHookId.PlayerFileCheckpoint);
        }

        /// <summary>
        /// Puts the craft file sequence the character file loaded with back on the entering player, after the engine has copied the
        /// locals of an earlier login over it. The login checks that read it call <see cref="NwnCraftSessionWorld.LoadedSequence"/>, which
        /// does the same, so they need not run after this.
        /// </summary>
        [PlayerSession(PlayerReload.LoginOnly,
            "Restores the file's craft sequence over a login's copied locals; a player who stays online keeps theirs.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyLoadedCraftSequence()
        {
            NwnCraftSessionWorld.ApplyLoadedSequence(GetEnteringObject());
        }
    }
}
