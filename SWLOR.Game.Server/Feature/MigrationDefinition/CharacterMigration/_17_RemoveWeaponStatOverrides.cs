using System;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Saved-data <see cref="WeaponStatOverrideMigration.MigrateObject"/>: removes Accuracy Stat and Damage Stat
/// overrides from the character's tradeable attack weapons, worn, carried, in containers, and in the
/// equipment and inventory of droids saved on controller items.
/// </summary>
internal sealed class _17_RemoveWeaponStatOverrides : ICharacterMigration
{
    private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";

    public int Version => 17;

    public void Migrate(SavedCharacter character)
    {
        foreach (var item in character.Equipped.Concat(character.CarriedRecursive).ToArray())
            MigrateItem(item);
    }

    private static bool MigrateItem(SavedItem item)
    {
        var changed = false;
        if (Item.IsAttackWeaponType(item.BaseItem) && !IsEconomyRestricted(item))
        {
            foreach (var property in item.Properties.Where(property =>
                         property.Type is ItemPropertyType.AccuracyStat or ItemPropertyType.DamageStat).ToArray())
            {
                item.RemoveProperty(property);
                changed = true;
            }
        }
        changed |= MigrateConstructedDroid(item);
        return changed;
    }

    /// <summary>
    /// Saved-data <see cref="Item.IsEconomyRestricted(uint)"/>. GetName reads a name saved only as a string
    /// reference from the talk table, whose item names are never empty or marked for NPCs; a stand-in name
    /// keeps the name rule from flagging those items.
    /// </summary>
    internal static bool IsEconomyRestricted(SavedItem item)
    {
        const string TalkTableName = "talk table name";
        var name = StoredObjectData.ReadLocalizedName(item.Node);
        if (string.IsNullOrEmpty(name) && StoredObjectData.ReadLocalizedNameReference(item.Node) != uint.MaxValue)
            name = TalkTableName;
        return Item.IsEconomyRestricted(item.BaseItem, name, item.GetLocalInt(Item.NoEconomyVariable) == 1,
            SavedCharacterData.WeaponHasInventoryIcon(item.BaseItem, item.SimpleModel));
    }

    private static bool MigrateConstructedDroid(SavedItem item)
    {
        var original = item.GetLocalString(ConstructedDroidVariable);
        if (string.IsNullOrWhiteSpace(original))
            return false;
        var droid = JsonConvert.DeserializeObject<ConstructedDroid>(original);
        if (droid == null)
            return false;
        var changed = MigrateDictionary(droid.EquippedItems);
        changed |= MigrateDictionary(droid.Inventory);
        if (changed)
            item.SetLocalString(ConstructedDroidVariable, JsonConvert.SerializeObject(droid));
        return changed;
    }

    private static bool MigrateDictionary<TKey>(System.Collections.Generic.Dictionary<TKey, string> items)
    {
        if (items == null)
            return false;
        var changed = false;
        foreach (var key in items.Keys.ToList())
        {
            if (string.IsNullOrWhiteSpace(items[key]) || StoredObjectData.IsCreatureData(items[key]))
                continue;
            var document = new StoredObjectData(Convert.FromBase64String(items[key]));
            var root = new SavedItem(document.Root, null, null);
            var itemChanged = false;
            foreach (var nested in new[] { root }.Concat(Expand(root)))
                itemChanged |= MigrateItem(nested);
            if (!itemChanged)
                continue;
            items[key] = Convert.ToBase64String(document.SerializeBytes());
            changed = true;
        }
        return changed;
    }

    private static System.Collections.Generic.IEnumerable<SavedItem> Expand(SavedItem item) =>
        item.Contents.SelectMany(content => new[] { content }.Concat(Expand(content)));
}
