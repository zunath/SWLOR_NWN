using SWLOR.Shared.Domain.Associate.Events;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Combat.EventHandlers
{
    internal class CombatServiceEventHandlers
    {
        private readonly IStatService _statService;

        public CombatServiceEventHandlers(
            IStatService statService,
            IEventAggregator eventAggregator)
        {
            _statService = statService;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleEnter>(e => ApplyPlayerStats());
            eventAggregator.Subscribe<OnAssociateStateEffect>(e => ReapplyFoodHP());
        }

        /// <summary>
        /// When a player enters the server, reapply HP and temporary stats.
        /// </summary>
        public void ApplyPlayerStats()
        {
            _statService.ApplyPlayerStats();
        }

        /// <summary>
        /// After a player's status effects are reassociated,
        /// adjust any food HP if necessary.
        /// </summary>
        public void ReapplyFoodHP()
        {
            _statService.ReapplyFoodHP();
        }
    }
}
