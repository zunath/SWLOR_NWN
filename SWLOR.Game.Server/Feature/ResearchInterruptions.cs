using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Ends research experiments that cannot continue. A player who logs out, or logs in with an experiment still open after a crash
    /// or restart, leaves it with the evidence saved so far, exactly as closing the window does.
    /// </summary>
    public class ResearchInterruptions
    {
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void SettleOnLogout()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            ResearchExperimentService.SettleOpenExperiments(GetObjectUUID(player));
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void SettleOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            ResearchExperimentService.SettleOpenExperiments(GetObjectUUID(player));
        }
    }
}
