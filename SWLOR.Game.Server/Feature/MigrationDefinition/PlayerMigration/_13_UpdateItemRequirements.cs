namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _13_UpdateItemRequirements : PlayerMigrationBase
    {
        public override int Version => 13;

        public override void Migrate(uint player)
        {
            EquipmentRequirementMigration.MigrateObject(player);
        }
    }
}
