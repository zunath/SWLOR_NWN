using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Skill.Contracts;

namespace SWLOR.Component.Migration.Feature.PlayerMigration
{
    public class _3_AdjustPlayerSpeeds: PlayerMigrationBase
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IAbilityService> _abilityService;
        
        private IAbilityService AbilityService => _abilityService.Value;

        public _3_AdjustPlayerSpeeds(
            ILogger logger, 
            IDatabaseService database, 
            IStatService statService, 
            ISkillService skillService, 
            ICombatService combatService, 
            IPerkService perkService, 
            IItemService itemService,
            IServiceProvider serviceProvider,
            ICreaturePluginService creaturePlugin) 
            : base(logger, database, statService, skillService, combatService, perkService, itemService, creaturePlugin)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _abilityService = new Lazy<IAbilityService>(() => _serviceProvider.GetRequiredService<IAbilityService>());
        }

        public override int Version => 3;
        public override void Migrate(uint player)
        {
            AbilityService.ToggleAbility(player, AbilityToggleType.Dash, false);

            CreaturePlugin.SetMovementRate(player, MovementRateType.PC);
            CreaturePlugin.SetMovementRateFactor(player, 1.0f);
        }
    }
}
