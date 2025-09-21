using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _3_AdjustPlayerSpeeds: IPlayerMigration
    {
        private readonly IAbilityService _abilityService;

        public _3_AdjustPlayerSpeeds(IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public int Version => 3;
        public void Migrate(uint player)
        {
            _abilityService.ToggleAbility(player, AbilityToggleType.Dash, false);

            CreaturePlugin.SetMovementRate(player, MovementRate.PC);
            CreaturePlugin.SetMovementRateFactor(player, 1.0f);
        }
    }
}
