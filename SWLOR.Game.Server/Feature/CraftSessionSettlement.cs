using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>Settles a craft in progress as a failure when its player dies, or when they log in with one still saved.</summary>
    public class CraftSessionSettlement
    {
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void SettleOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            CraftSessionService.SettleLeftover(player);
        }

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void SettleOnDeath()
        {
            var player = GetLastPlayerDied();
            if (!GetIsPC(player))
                return;

            CraftSessionService.Fail(player, CraftSessionEndReason.Died);
        }
    }
}
