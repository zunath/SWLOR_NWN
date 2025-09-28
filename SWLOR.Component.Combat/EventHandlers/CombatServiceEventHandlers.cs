using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Associate;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Combat.EventHandlers
{
    internal class CombatServiceEventHandlers
    {
        private readonly IStatService _statService;

        public CombatServiceEventHandlers(IStatService statService)
        {
            _statService = statService;
        }

        /// <summary>
        /// When a player enters the server, reapply HP and temporary stats.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void ApplyPlayerStats()
        {
            _statService.ApplyPlayerStats();
        }

        /// <summary>
        /// After a player's status effects are reassociated,
        /// adjust any food HP if necessary.
        /// </summary>
        [ScriptHandler<OnAssociateStateEffect>]
        public void ReapplyFoodHP()
        {
            _statService.ReapplyFoodHP();
        }
    }
}
