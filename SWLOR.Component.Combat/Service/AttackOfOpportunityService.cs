using SWLOR.Component.Combat.Contracts;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Component.Combat.Service
{
    public class AttackOfOpportunityService : IAttackOfOpportunityService
    {
        /// <summary>
        /// Whenever an attack of opportunity is broadcast, skip the event to disable it.
        /// This should effectively disable AOOs across the board.
        /// </summary>
        public void OnAttackOfOpportunity()
        {
            EventsPlugin.SkipEvent();
        }
    }
}
