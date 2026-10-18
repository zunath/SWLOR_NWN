using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Saved-data counterparts of the combat upgrade's player item rules that rebuild record totals or move
/// equipment: <see cref="CombatReadinessMigration"/>, <see cref="PistolBaseItemMigration"/> and
/// <see cref="AccuracyItemPropertyMigration"/>.
/// </summary>
internal static class SavedCombatUpgradeItemMigrations
{
    private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";
    private const string DroidItemIdVariable = "DROID_ITEM_ID";

    // ---- Combat readiness -----------------------------------------------------------------------------

    /// <summary>
    /// Renames combat readiness enhancements everywhere on the character (as <see cref="CombatReadinessMigration.MigrateObject"/>
    /// does for live items), then sets the record's combat readiness to what its equipment grants.
    /// </summary>
    public static void MigrateCombatReadiness(SavedCharacter character)
    {
        foreach (var item in AllItems(character))
            MigrateCombatReadinessItem(item);
        if (character.AppliesRecordEffects)
            character.Record.CombatReadiness = character.Equipped.Sum(item => item.Properties
                .Where(property => property.Type == ItemPropertyType.CombatReadiness).Sum(property => property.CostValue));
    }

    private static bool MigrateCombatReadinessItem(SavedItem item)
    {
        var changed = false;
        // GetName reads a name saved only as a talk table reference from the talk table; renaming it is harmless.
        if (CombatReadinessMigration.TryGetCombatReadinessItemName(item.ResRef, out var name) && item.Name != name)
        {
            item.SetName(name);
            changed = true;
        }
        changed |= MigrateDroid(item, MigrateCombatReadinessItem);
        return changed;
    }

    // ---- Accuracy -------------------------------------------------------------------------------------

    /// <summary>
    /// Combines native Accuracy and Enhancement Bonus properties into Accuracy everywhere on the character (as
    /// <see cref="AccuracyItemPropertyMigration.MigrateObject"/> does for live items), then sets the record's accuracy
    /// to what its equipped non-weapon items grant.
    /// </summary>
    public static void MigrateAccuracy(SavedCharacter character)
    {
        foreach (var item in AllItems(character))
            MigrateAccuracyItem(item, false);
        if (!character.AppliesRecordEffects)
            return;
        character.Record.Accuracy = character.Equipped
            .Where(item => !Item.IsAttackWeaponType(item.BaseItem) && item.BaseItem != BaseItem.CreatureItem)
            .Sum(item => item.Properties.Where(property => property.Type == ItemPropertyType.Accuracy).Sum(property => property.CostValue));
    }

    /// <summary>
    /// The live rule first sees an item after loading converted its retired rows (<see cref="SavedLegacyItemProperties"/>);
    /// items saved inside a droid load only when the rule reads them, so they are converted here first.
    /// </summary>
    private static bool MigrateAccuracyItem(SavedItem item, bool convertOnLoad)
    {
        var changed = convertOnLoad && SavedLegacyItemProperties.Convert(item);
        if (item.GetLocalInt(Native.LegacyItemProperties.ConvertedVariable) > 0)
        {
            item.DeleteLocal(Native.LegacyItemProperties.ConvertedVariable, 1);
            changed = true;
        }
        var legacy = item.Properties.Where(property => AccuracyItemPropertyMigration.IsLegacyAccuracyProperty(property.Type)).ToList();
        if (legacy.Count > 0)
        {
            var accuracy = AccuracyItemPropertyMigration.CombineLegacyAccuracy(legacy.Select(property => property.CostValue));
            foreach (var property in legacy)
                item.RemoveProperty(property);
            if (accuracy > 0)
                item.AddProperty(ItemPropertyType.Accuracy, -1, accuracy);
            changed = true;
        }
        changed |= MigrateDroid(item, droidItem => MigrateAccuracyItem(droidItem, true));
        return changed;
    }

    // ---- Pistols and ammunition -----------------------------------------------------------------------

    /// <summary>
    /// Gives pistols and blaster ammunition their canonical base items (as <see cref="PistolBaseItemMigration.MigrateStoredObject"/>
    /// does for live objects) and moves ammunition worn in the arrow slot to the bullet slot. When the worn weapon would
    /// make the engine supply bullets, the moved stack stays in the inventory, as the live rule does.
    /// </summary>
    public static void MigratePistols(SavedCharacter character)
    {
        var legacyAmmo = character.GetEquipped(InventorySlot.Arrows);
        var generatedBullets = WouldGenerateBullets(character);
        foreach (var item in character.Equipped.ToArray())
        {
            if (item.Node == legacyAmmo?.Node)
                continue;
            NormalizePistolItem(item);
        }
        foreach (var item in character.CarriedRecursive.ToArray())
            NormalizePistolItem(item);

        if (legacyAmmo == null)
            return;
        var originalBaseItem = legacyAmmo.BaseItem;
        var canonicalSlot = originalBaseItem == BaseItem.Bullet
            ? InventorySlot.Bullets
            : PistolBaseItemCompatibility.GetCanonicalInventorySlot(originalBaseItem, InventorySlot.Arrows);
        NormalizePistolItem(legacyAmmo);
        if (canonicalSlot != InventorySlot.Bullets)
            return;
        if (generatedBullets)
        {
            character.Unequip(legacyAmmo);
            return;
        }
        var existing = character.GetEquipped(InventorySlot.Bullets);
        if (existing != null)
            character.Unequip(existing);
        character.MoveEquipped(legacyAmmo, InventorySlot.Bullets);
    }

    /// <summary>
    /// Whether loading the character gives it engine-owned bullets: a worn sling-based pistol with Unlimited
    /// Ammunition and nothing in the bullet slot.
    /// </summary>
    private static bool WouldGenerateBullets(SavedCharacter character)
    {
        var weapon = character.GetEquipped(InventorySlot.RightHand);
        return weapon != null && weapon.BaseItem == BaseItem.Sling && character.GetEquipped(InventorySlot.Bullets) == null &&
               weapon.Properties.Any(property => property.Type == ItemPropertyType.UnlimitedAmmunition);
    }

    private static bool NormalizePistolItem(SavedItem item)
    {
        var changed = false;
        var canonical = PistolBaseItemCompatibility.GetCanonicalBaseItem(item.BaseItem, item.ResRef);
        if (canonical != item.BaseItem)
        {
            item.BaseItem = canonical;
            changed = true;
        }
        changed |= NormalizePistolDroid(item);
        return changed;
    }

    /// <summary>The droid half of <see cref="PistolBaseItemMigration"/>: converts saved droid items and moves arrow-slot ammunition.</summary>
    private static bool NormalizePistolDroid(SavedItem controller)
    {
        var serialized = controller.GetLocalString(ConstructedDroidVariable);
        if (string.IsNullOrWhiteSpace(serialized))
            return false;
        var droid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
        if (droid == null)
            return false;
        var changed = false;
        string Convert(string data, out BaseItem canonical)
        {
            var result = BaseItem.Invalid;
            if (SavedItemDocument.Migrate(data, document =>
                {
                    var itemChanged = false;
                    foreach (var item in document.AllItems)
                        itemChanged |= NormalizePistolItem(item);
                    result = document.Root.BaseItem;
                    return itemChanged;
                }, out var migrated))
                changed = true;
            else if (!string.IsNullOrWhiteSpace(data) && !StoredObjectData.IsCreatureData(data))
                result = new SavedItemDocument(data).Root.BaseItem;
            canonical = result;
            return migrated;
        }
        droid.SerializedCPU = Convert(droid.SerializedCPU, out _);
        droid.SerializedHead = Convert(droid.SerializedHead, out _);
        droid.SerializedBody = Convert(droid.SerializedBody, out _);
        droid.SerializedArms = Convert(droid.SerializedArms, out _);
        droid.SerializedLegs = Convert(droid.SerializedLegs, out _);
        if (droid.Inventory != null)
            foreach (var key in droid.Inventory.Keys.ToList())
                droid.Inventory[key] = Convert(droid.Inventory[key], out _);
        if (droid.EquippedItems != null)
        {
            var moveLegacyAmmo = false;
            foreach (var slot in droid.EquippedItems.Keys.ToList())
            {
                droid.EquippedItems[slot] = Convert(droid.EquippedItems[slot], out var canonical);
                if (slot == InventorySlot.Arrows && canonical == BaseItem.Bullet)
                    moveLegacyAmmo = true;
            }
            if (moveLegacyAmmo)
            {
                var ammo = droid.EquippedItems[InventorySlot.Arrows];
                if (droid.EquippedItems.TryGetValue(InventorySlot.Bullets, out var existing))
                    MoveToDroidInventory(droid, existing);
                droid.EquippedItems.Remove(InventorySlot.Arrows);
                droid.EquippedItems[InventorySlot.Bullets] = ammo;
                changed = true;
            }
        }
        if (changed)
            controller.SetLocalString(ConstructedDroidVariable, JsonConvert.SerializeObject(droid));
        return changed;
    }

    /// <summary>Stows displaced droid ammunition under a collision-free inventory key, keeping its saved identity.</summary>
    private static void MoveToDroidInventory(ConstructedDroid droid, string serialized)
    {
        droid.Inventory ??= new Dictionary<string, string>();
        var id = string.Empty;
        if (!string.IsNullOrWhiteSpace(serialized) && !StoredObjectData.IsCreatureData(serialized))
        {
            var document = new SavedItemDocument(serialized);
            id = document.Root.GetLocalString(DroidItemIdVariable);
            if (string.IsNullOrWhiteSpace(id) || droid.Inventory.ContainsKey(id))
            {
                id = Guid.NewGuid().ToString();
                document.Root.SetLocalString(DroidItemIdVariable, id);
                serialized = document.Serialize();
            }
        }
        if (string.IsNullOrWhiteSpace(id))
            id = Guid.NewGuid().ToString();
        droid.Inventory[id] = serialized;
    }

    // ---- Shared ---------------------------------------------------------------------------------------

    private static IEnumerable<SavedItem> AllItems(SavedCharacter character) =>
        character.Equipped.Concat(character.CarriedRecursive).ToArray();

    /// <summary>Applies an item rule to each part, worn item and inventory item of the droid saved on a controller.</summary>
    private static bool MigrateDroid(SavedItem controller, Func<SavedItem, bool> migrate)
    {
        var serialized = controller.GetLocalString(ConstructedDroidVariable);
        if (string.IsNullOrWhiteSpace(serialized))
            return false;
        var droid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
        if (droid == null)
            return false;
        var changed = false;
        string Migrate(string data)
        {
            if (!SavedItemDocument.Migrate(data, document => document.AllItems.Aggregate(false, (any, item) => migrate(item) | any), out var migrated))
                return data;
            changed = true;
            return migrated;
        }
        droid.SerializedCPU = Migrate(droid.SerializedCPU);
        droid.SerializedHead = Migrate(droid.SerializedHead);
        droid.SerializedBody = Migrate(droid.SerializedBody);
        droid.SerializedArms = Migrate(droid.SerializedArms);
        droid.SerializedLegs = Migrate(droid.SerializedLegs);
        if (droid.EquippedItems != null)
            foreach (var key in droid.EquippedItems.Keys.ToList())
                droid.EquippedItems[key] = Migrate(droid.EquippedItems[key]);
        if (droid.Inventory != null)
            foreach (var key in droid.Inventory.Keys.ToList())
                droid.Inventory[key] = Migrate(droid.Inventory[key]);
        if (changed)
            controller.SetLocalString(ConstructedDroidVariable, JsonConvert.SerializeObject(droid));
        return changed;
    }
}
