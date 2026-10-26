using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Ends crafting that cannot continue. A saved craft settles as a failure when its player dies or logs in with one still
    /// saved, and a quick craft batch stops when its player dies or leaves. XP a death-failure earned is given at the respawn.
    /// </summary>
    public class CraftingInterruptions
    {
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void SettleOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            CraftSessionService.RecoverAtLogin(player);
        }

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void SettleOnDeath()
        {
            var player = GetLastPlayerDied();
            if (!GetIsPC(player))
                return;

            QuickCraftService.Interrupt(player);
            CraftSessionService.Fail(player, CraftSessionEndReason.Died);
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
                    CraftSessionService.GrantPendingXp(player);
            });
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void StopQuickCraftOnLogout()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player))
                return;

            QuickCraftService.Interrupt(player);
        }
    }
}
