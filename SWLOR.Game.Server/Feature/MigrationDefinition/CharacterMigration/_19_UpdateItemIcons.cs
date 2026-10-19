using System.Linq;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Saved-data <see cref="ItemIconMigration.MigrateObject"/>: updates the artwork of every equipped and carried
/// item and of the droids saved on controller items.
/// </summary>
internal sealed class _19_UpdateItemIcons : ICharacterMigration
{
    private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";

    public int Version => 19;

    public void Migrate(SavedCharacter character)
    {
        foreach (var item in character.Equipped.Concat(character.CarriedRecursive).ToArray())
        {
            StoredObjectData.UpdateArtwork(item.Node);
            var droid = item.GetLocalString(ConstructedDroidVariable);
            if (ItemIconMigration.MigrateDroidArtwork(droid, StoredObjectData.MigrateSavedArtwork, out var updated))
                item.SetLocalString(ConstructedDroidVariable, updated);
        }
    }
}
