using SWLOR.Component.Migration.Contracts;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;

namespace SWLOR.Component.Migration.Feature.PlayerMigration
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
