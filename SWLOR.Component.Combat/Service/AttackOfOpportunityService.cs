using SWLOR.Component.Combat.Contracts;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.Service;

namespace SWLOR.Component.Combat.Service
{
    public class AttackOfOpportunityService : IAttackOfOpportunityService
    {
        private readonly IEventsPluginService _eventsPlugin;

        public AttackOfOpportunityService(IEventsPluginService eventsPlugin)
        {
            _eventsPlugin = eventsPlugin;
        }

        /// <summary>
        /// Whenever an attack of opportunity is broadcast, skip the event to disable it.
        /// This should effectively disable AOOs across the board.
        /// </summary>
        public void OnAttackOfOpportunity()
        {
            _eventsPlugin.SkipEvent();
        }
    }
}
