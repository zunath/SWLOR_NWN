using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Crafting.Research;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Ends research experiments that cannot continue. A player who logs out, or logs in with an experiment still open after a crash
    /// or restart, leaves it with the evidence saved so far, exactly as closing the window does. At login, jobs are first checked
    /// against the character file, in case a crash lost a start or a blueprint hand-back.
    /// </summary>
    public class ResearchInterruptions
    {
        [PlayerSession(PlayerReload.LogoutOnly,
            "Settles open experiments with a roll at logout; they stay open in Redis and the player carries on.")]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void SettleOnLogout()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            CraftResearchExperiment.SettleOpenExperiments(GetObjectUUID(player));
        }

        [PlayerSession(PlayerReload.LoginOnly,
            "Settles research a crash left open in a freshly loaded character file; on a player who stays online it would delete jobs started this session.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void SettleOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            CraftResearchExperiment.RecoverAtLogin(player);
            CraftResearchExperiment.SettleOpenExperiments(GetObjectUUID(player));
        }
    }
}
