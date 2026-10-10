using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public sealed class _16_RetireForceWeaponEnhancements : PlayerMigrationBase
    {
        public override int Version => 16;

        public override void Migrate(uint player) => ForceWeaponEnhancementMigration.MigrateObject(player);

        public override void MigratePlayerData(Player player) => ForceWeaponEnhancementMigration.MigrateRecipeKnowledge(player);
    }
}
