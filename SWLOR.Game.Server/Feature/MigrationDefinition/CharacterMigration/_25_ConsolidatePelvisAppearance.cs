using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

internal sealed class _25_ConsolidatePelvisAppearance : ICharacterMigration
{
    public int Version => 25;

    public void Migrate(SavedCharacter character)
    {
        var visited = new HashSet<StoredObjectData.Node>();
        StoredObjectData.MigratePelvisNode(character.Root, false, visited);
        // Previous upgrades may have stowed items in overflow before this migration runs.
        foreach (var item in character.Carried.Concat(character.Equipped))
            StoredObjectData.MigratePelvisNode(item.Node, true, visited);
    }

    public void MigratePlayerData(Player player) { }
}
