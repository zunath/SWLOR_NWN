using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

public sealed class _20_MimicryLoadouts : PlayerMigrationBase
{
    public override int Version => 20;
    public override void Migrate(uint player) { }
    public override void MigratePlayerData(Player player) => MimicryLoadouts.EnsureDefault(player);
}
