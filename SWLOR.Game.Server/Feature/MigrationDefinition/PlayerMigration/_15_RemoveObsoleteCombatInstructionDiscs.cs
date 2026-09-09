using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CurrencyService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _15_RemoveObsoleteCombatInstructionDiscs : PlayerMigrationBase
    {
        public override int Version => 15;

        public override void Migrate(uint player)
        {
            ObsoleteItemMigration.RemoveObsoleteItemsFromObject(player);
            LegacySaberMigration.MigratePlayer(player);
            PlayerInitialization.ResetFeatsToBaseline(player);
        }

        public override void MigratePlayerData(Player player)
        {
            if (!player.Currencies.ContainsKey(CurrencyType.RebuildToken))
                player.Currencies[CurrencyType.RebuildToken] = 0;

            player.Currencies[CurrencyType.RebuildToken]++;
        }
    }
}
