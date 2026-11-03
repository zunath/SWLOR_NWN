using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>Moves carried incubation enzymes to the 1x1 enzyme base item (<see cref="EnzymeBaseItemMigration"/>).</summary>
internal sealed class _26_ShrinkEnzymes : ICharacterMigration
{
    public int Version => 26;

    public void Migrate(SavedCharacter character)
    {
        var visited = new HashSet<StoredObjectData.Node>();
        StoredObjectData.MigrateEnzymeNode(character.Root, false, visited);
        // Items waiting in overflow storage hang off the character separately from its inventory lists.
        foreach (var item in character.Carried.Concat(character.Equipped))
            StoredObjectData.MigrateEnzymeNode(item.Node, true, visited);
    }
}
