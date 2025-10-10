using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Skill.Contracts;

namespace SWLOR.Component.Migration.Definitions.PlayerMigration
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
            IStatCalculationService statCalculationService,
            ISkillService skillService,
            IPerkService perkService,
            IItemService itemService,
            IServiceProvider serviceProvider,
            ICreaturePluginService creaturePlugin,
            IStatApplicationService statApplicationService)
            : base(
                logger, 
                database, 
                statCalculationService, 
                skillService,
                perkService,
                itemService, 
                creaturePlugin, 
                statApplicationService)
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


