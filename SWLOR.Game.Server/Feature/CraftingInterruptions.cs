using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Crafting.Minigame;
using SWLOR.Game.Server.Domain.Crafting;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Ends crafting that cannot continue. A saved craft settles as a failure when its player dies or logs in with one still
    /// saved, and a quick craft batch stops when its player dies or leaves. XP a death-failure earned is given at the respawn.
    /// </summary>
    public class CraftingInterruptions
    {
        /// <summary>
        /// Settles what a crash or logout left between the player record and the character file: the craft session first, then the
        /// Prospecting harvest, then the Search finds. They run in this one handler, in this order, because each login step that exports
        /// writes the file's sequence: the Search return's export must come after the craft and harvest recoveries have put their items
        /// back, or a crash right after it would leave a file whose sequence claims transfers it does not hold.
        /// </summary>
        [PlayerSession(PlayerReload.LoginOnly,
            "Settles crafts a crash left open in a freshly loaded character file; on a player who stays online it would duplicate items.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void SettleOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            CraftSessions.RecoverAtLogin(player);
            NwnProspectingWorld.Instance.ReturnPendingHarvest(player);
            NwnScavengeWorld.Instance.ReturnPendingFinds(player, atLogin: true);
        }

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void SettleOnDeath()
        {
            var player = GetLastPlayerDied();
            if (!GetIsPC(player))
                return;

            CraftQuickCraft.Interrupt(player);
            CraftSessions.Fail(player, CraftSessionEndReason.Died);
        }

        [NWNEventHandler(ScriptName.OnModuleRespawn)]
        public static void GrantPendingXpOnRespawn()
        {
            var player = GetLastRespawnButtonPresser();
            if (!GetIsPC(player))
                return;

            // The resurrection from the respawn button is applied by another handler of the same event, so look once it has landed.
            DelayCommand(1f, () =>
            {
                if (GetIsObjectValid(player))
                    CraftSessions.GrantPendingXp(player);
            });
        }

        [PlayerSession(PlayerReload.Quiesce,
            "A quick-craft batch cannot be carried (its timer is a closure), so it stops as at logout and the craft immobilize comes off instead of sticking.",
            Step = nameof(StopQuickCraftFor))]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void StopQuickCraftOnLogout()
        {
            StopQuickCraftFor(GetExitingObject());
        }

        /// <summary>
        /// The work of <see cref="StopQuickCraftOnLogout"/> for <paramref name="player"/>: the logout's handler, and a hot reload's quiescence step for a player who stays online.
        /// </summary>
        public static void StopQuickCraftFor(uint player)
        {
            if (!GetIsPC(player))
                return;

            CraftQuickCraft.Interrupt(player);
        }
    }
}
