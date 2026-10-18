using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Service.MigrationService;
using Node = SWLOR.Game.Server.Feature.MigrationDefinition.StoredObjectData.Node;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Saved-data <see cref="DungeonSaberMigration.MigrateObject(uint)"/>: replaces retired dungeon saber drops with
/// their compensation items on the character, in carried containers and in the droids saved on controller items.
/// </summary>
internal sealed class _18_ReplaceDungeonSabers : ICharacterMigration
{
    private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";
    private const string DroidItemIdVariable = "DROID_ITEM_ID";

    public int Version => 18;

    public void Migrate(SavedCharacter character)
    {
        foreach (var item in character.Equipped.Concat(character.Carried).ToArray())
            MigrateItem(character, item);
    }

    private static void MigrateItem(SavedCharacter character, SavedItem item)
    {
        if (DungeonSaberMigration.TryGetReplacement(item.ResRef, out var replacement))
        {
            character.Replace(item, replacement);
            return;
        }
        MigrateDroid(item);
        foreach (var content in item.Contents.ToArray())
            MigrateItem(character, content);
    }

    private static void MigrateDroid(SavedItem item)
    {
        var original = item.GetLocalString(ConstructedDroidVariable);
        if (string.IsNullOrWhiteSpace(original))
            return;
        var droid = JObject.Parse(original);
        var changed = false;
        foreach (var field in new[] { "SerializedCPU", "SerializedHead", "SerializedBody", "SerializedArms", "SerializedLegs" })
        {
            if (!MigrateSerializedItem((string)droid[field], out var migrated, out _)) continue;
            droid[field] = migrated;
            changed = true;
        }
        foreach (var field in new[] { "EquippedItems", "Inventory" })
        {
            if (droid[field] is not JObject items) continue;
            foreach (var entry in items.Properties().ToArray())
            {
                if (!MigrateSerializedItem((string)entry.Value, out var migrated, out var replacedRoot)) continue;
                if (field == "EquippedItems" && replacedRoot)
                {
                    // A cowl, sash or vambrace cannot remain in a saved weapon slot.
                    var id = StoredObjectData.ReadRootString(migrated, DroidItemIdVariable);
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        id = Guid.NewGuid().ToString();
                        migrated = StoredObjectData.SetRootString(migrated, DroidItemIdVariable, id);
                    }
                    var inventory = droid["Inventory"] as JObject ?? new JObject();
                    if (inventory.ContainsKey(id))
                        throw new InvalidOperationException("A converted droid item already has an inventory entry.");
                    inventory[id] = migrated;
                    droid["Inventory"] = inventory;
                    entry.Remove();
                }
                else entry.Value = migrated;
                changed = true;
            }
        }
        if (changed)
            item.SetLocalString(ConstructedDroidVariable, droid.ToString(Formatting.None));
    }

    /// <summary>A saved droid item: replaced outright when retired, otherwise with its retired contents replaced.</summary>
    private static bool MigrateSerializedItem(string data, out string migrated, out bool replacedRoot)
    {
        migrated = data;
        replacedRoot = false;
        if (string.IsNullOrWhiteSpace(data))
            return false;
        var document = new StoredObjectData(Convert.FromBase64String(data));
        var root = new SavedItem(document.Root, null, null);
        if (DungeonSaberMigration.TryGetReplacement(root.ResRef, out var replacement))
        {
            var created = StoredObjectData.ParseItem(SavedCharacterData.GetBlueprint(replacement));
            var item = new SavedItem(created, null, null) { StackSize = Math.Max(1, root.StackSize) };
            var droidItemId = root.GetLocalString(DroidItemIdVariable);
            if (!string.IsNullOrWhiteSpace(droidItemId))
                item.SetLocalString(DroidItemIdVariable, droidItemId);
            migrated = StoredObjectData.ItemToUti(created);
            replacedRoot = true;
            return true;
        }
        var changed = MigrateContents(root);
        var droid = root.GetLocalString(ConstructedDroidVariable);
        MigrateDroid(root);
        changed |= root.GetLocalString(ConstructedDroidVariable) != droid;
        if (changed)
            migrated = Convert.ToBase64String(document.SerializeBytes());
        return changed;
    }

    private static bool MigrateContents(SavedItem container)
    {
        var changed = false;
        foreach (var content in container.Contents.ToArray())
        {
            if (DungeonSaberMigration.TryGetReplacement(content.ResRef, out var replacement))
            {
                var created = StoredObjectData.ParseItem(SavedCharacterData.GetBlueprint(replacement));
                var item = new SavedItem(created, null, null) { StackSize = Math.Max(1, content.StackSize) };
                var droidItemId = content.GetLocalString(DroidItemIdVariable);
                if (!string.IsNullOrWhiteSpace(droidItemId))
                    item.SetLocalString(DroidItemIdVariable, droidItemId);
                if (!SavedCharacter.Place(content.List, SavedCharacterData.ContainerWidth, SavedCharacterData.ContainerHeight, created))
                    throw new InvalidOperationException($"A saved droid container has no room for replacement '{replacement}'.");
                content.List.Children.Remove(content.Node);
                StoredObjectData.NumberListElements(content.List);
                changed = true;
                continue;
            }
            changed |= MigrateContents(content);
            var droid = content.GetLocalString(ConstructedDroidVariable);
            MigrateDroid(content);
            changed |= content.GetLocalString(ConstructedDroidVariable) != droid;
        }
        return changed;
    }
}
