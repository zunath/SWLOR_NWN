using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CurrencyService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Gives an existing character one rebuild token. Call it only from a character migration's
/// <see cref="Service.MigrationService.ICharacterMigration.MigratePlayerData"/>, whose record version
/// checkpoint grants it once per release.
/// </summary>
internal static class RebuildTokenGrant
{
    public static void Grant(Player player)
    {
        player.Currencies.TryGetValue(CurrencyType.RebuildToken, out var tokens);
        player.Currencies[CurrencyType.RebuildToken] = tokens + 1;
    }
}
