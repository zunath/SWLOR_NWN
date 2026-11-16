using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Adds two rebuild tokens to existing characters to catch them up to the extra 2 rebuild tokens granted on creation.
/// </summary>
internal sealed class _26_AddRebuildTokens : ICharacterMigration
{
    public int Version => 26;

    public void Migrate(SavedCharacter character) {}

    public void MigratePlayerData(Player player)
    {
        RebuildTokenGrant.Grant(player);
        RebuildTokenGrant.Grant(player);
    }
}
