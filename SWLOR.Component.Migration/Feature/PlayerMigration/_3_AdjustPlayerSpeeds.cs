using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Common.Contracts;

namespace SWLOR.Component.Migration.Feature.PlayerMigration
{
    public class _3_AdjustPlayerSpeeds: PlayerMigrationBase
    {
        private readonly IAbilityService _abilityService;

        public _3_AdjustPlayerSpeeds(
            ILogger logger, 
            IDatabaseService database, 
            IStatService statService, 
            ISkillService skillService, 
            ICombatService combatService, 
            IPerkService perkService, 
            IItemService itemService,
            IAbilityService abilityService) 
            : base(logger, database, statService, skillService, combatService, perkService, itemService)
        {
            _abilityService = abilityService;
        }

        public override int Version => 3;
        public override void Migrate(uint player)
        {
            _abilityService.ToggleAbility(player, AbilityToggleType.Dash, false);

            CreaturePlugin.SetMovementRate(player, MovementRate.PC);
            CreaturePlugin.SetMovementRateFactor(player, 1.0f);
        }
    }
}
