
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Core.Event;

namespace SWLOR.Game.Server.Feature
{
    public static class DisableAttackOfOpportunity
    {
        /// <summary>
        /// Whenever an attack of opportunity is broadcast, skip the event to disable it.
        /// This should effectively disable AOOs across the board.
        /// </summary>
        [ScriptHandler(ScriptName.OnBroadcastAttackOfOpportunityBefore)]
        public static void OnAttackOfOpportunity()
        {
            EventsPlugin.SkipEvent();
        }
    }
}
