using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;

namespace SWLOR.Game.Server.Feature
{
    public static class DisableAttackOfOpportunity
    {
        /// <summary>
        /// Whenever an attack of opportunity is broadcast, skip the event to disable it.
        /// This should effectively disable AOOs across the board.
        /// </summary>
        [NWNEventHandler(ScriptName.OnBroadcastAttackOfOpportunityBefore)]
        public static void OnAttackOfOpportunity()
        {
            EventsPlugin.SkipEvent();
        }
    }
}
