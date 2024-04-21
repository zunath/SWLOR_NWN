using SWLOR.Core.NWNX;
using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.Service;
using SWLOR.Core.Service.AbilityService;
using SWLOR.Core.Service.MigrationService;

namespace SWLOR.Core.Feature.MigrationDefinition.PlayerMigration
{
    public class _3_AdjustPlayerSpeeds: IPlayerMigration
    {
        public int Version => 3;
        public void Migrate(uint player)
        {
            Ability.ToggleAbility(player, AbilityToggleType.Dash, false);

            CreaturePlugin.SetMovementRate(player, MovementRate.PC);
            CreaturePlugin.SetMovementRateFactor(player, 1.0f);
        }
    }
}
