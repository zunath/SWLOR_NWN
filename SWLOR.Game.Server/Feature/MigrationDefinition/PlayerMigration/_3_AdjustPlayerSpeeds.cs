using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
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
