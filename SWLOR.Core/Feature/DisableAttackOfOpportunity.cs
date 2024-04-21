using SWLOR.Core.NWNX;

namespace SWLOR.Core.Feature
{
    public static class DisableAttackOfOpportunity
    {
        /// <summary>
        /// Whenever an attack of opportunity is broadcast, skip the event to disable it.
        /// This should effectively disable AOOs across the board.
        /// </summary>
        [NWNEventHandler("brdcast_aoo_bef")]
        public static void OnAttackOfOpportunity()
        {
            EventsPlugin.SkipEvent();
        }
    }
}
