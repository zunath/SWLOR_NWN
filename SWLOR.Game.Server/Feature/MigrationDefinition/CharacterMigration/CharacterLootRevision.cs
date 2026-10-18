using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Node = SWLOR.Game.Server.Feature.MigrationDefinition.StoredObjectData.Node;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Saved-data <see cref="LootStatRevisionMigration.MigrateObject(uint)"/> for a player character: repairs legacy
/// headgear and wearable types, optionally updates artwork, and revises loot stats on equipped and carried items
/// and the droids saved on controller items. An equipped item's revised stats change the record by their
/// difference, as on a logged-in wearer.
/// </summary>
internal static class CharacterLootRevision
{
    public static bool Migrate(SavedCharacter character, bool updateArtwork)
    {
        var pools = CapturePools(character);
        var visited = new HashSet<Node>();
        var changed = ConvertHeadgear(character, character.GetEquipped(InventorySlot.Chest));
        for (var slot = 0; slot < NumberOfInventorySlots; slot++)
        {
            var item = character.GetEquipped((InventorySlot)slot);
            if (item != null)
                changed |= MigrateItem(character, item, visited, updateArtwork);
        }
        foreach (var item in character.Carried.ToArray())
            changed |= MigrateItem(character, item, visited, updateArtwork);
        if (changed)
            RestorePools(character, pools);
        return changed;
    }

    private static bool MigrateItem(SavedCharacter character, SavedItem item, HashSet<Node> visited, bool updateArtwork)
    {
        if (!visited.Add(item.Node))
            return false;
        var changed = ConvertHeadgear(character, item);
        // A converted item may have moved to another slot or into the inventory.
        item = character.Locate(item.Node) ?? item;
        changed |= ConvertEquipmentType(character, item);
        item = character.Locate(item.Node) ?? item;
        if (updateArtwork)
            changed |= StoredObjectData.UpdateArtwork(item.Node);

        var wearer = item.IsEquipped;
        var revised = StoredObjectData.ReviseStats(item.Node, (change, difference) =>
        {
            // Retired elemental Defense never counted on the wearer.
            if (!wearer || change.Type == ItemPropertyType.Defense && !((CombatDamageType)change.Subtype).IsDefenseDamageType())
                return;
            var subtype = StoredObjectData.HasSubtype(change.Type) ? change.Subtype : -1;
            character.ApplyStatAmount(item, change.Type, subtype, Math.Abs(difference), difference > 0);
        });
        if (revised && wearer)
            character.RefreshCombatReadinessEquipment();
        changed |= revised;
        changed |= StoredObjectData.MigrateSavedDroid(item.Node, true, updateArtwork);

        foreach (var content in item.Contents.ToArray())
            changed |= MigrateItem(character, content, visited, updateArtwork);
        return changed;
    }

    /// <summary>
    /// Saved-data <see cref="HeadgearSlotMigration.ConvertItem"/>. Legacy headgear worn in the chest slot moves
    /// to an empty head slot; with the head slot taken it moves into the inventory, or overflow storage when
    /// the inventory is full. Its stats leave the record unless it is worn again.
    /// </summary>
    private static bool ConvertHeadgear(SavedCharacter character, SavedItem item)
    {
        if (item == null)
            return false;
        var resref = item.ResRef;
        var baseItem = item.BaseItem;
        if (baseItem == BaseItem.Helmet)
            return StoredObjectData.ConvertHeadgear(item.Node);
        if (!HeadgearSlotMigration.IsLegacyHeadgear(resref, baseItem))
            return false;
        var equipped = item.IsEquipped && item.Slot == InventorySlot.Chest;
        var pools = equipped ? CapturePools(character) : default;
        if (equipped)
            character.ApplyItemStats(item, false);
        StoredObjectData.ConvertHeadgear(item.Node);
        if (equipped)
        {
            MoveOrStow(character, item, InventorySlot.Head);
            RestorePools(character, pools);
            character.RefreshCombatReadinessEquipment();
        }
        return true;
    }

    /// <summary>Saved-data <see cref="EquipmentTypeMigration.ConvertItem"/>, moving worn items as <see cref="ConvertHeadgear"/> does.</summary>
    private static bool ConvertEquipmentType(SavedCharacter character, SavedItem item)
    {
        if (!EquipmentTypeMigration.IsLegacyEquipment(item.ResRef, item.BaseItem))
            return false;
        var correction = EquipmentTypeMigration.Corrections[item.ResRef];
        var equipped = item.IsEquipped && item.Slot == EquipmentTypeMigration.GetLegacySlot(correction.LegacyType);
        var pools = equipped ? CapturePools(character) : default;
        if (equipped)
            character.ApplyItemStats(item, false);
        StoredObjectData.ConvertEquipmentType(item.Node);
        if (equipped)
        {
            MoveOrStow(character, item, correction.Slot);
            RestorePools(character, pools);
            character.RefreshCombatReadinessEquipment();
        }
        return true;
    }

    /// <summary>Re-equips a worn item in its corrected slot when that slot is empty, otherwise stows it.</summary>
    private static void MoveOrStow(SavedCharacter character, SavedItem item, InventorySlot slot)
    {
        if (character.GetEquipped(slot) == null)
        {
            var moved = character.MoveEquipped(item, slot);
            character.ApplyItemStats(moved, true);
        }
        else
            character.Unequip(item);
    }

    /// <summary>
    /// The record's FP and STM before equipped items change. HP needs no capture: login sets it from the record,
    /// which equipment changes do not touch (<see cref="SavedCharacter.SetSavedHitPointsToMaximum"/>).
    /// </summary>
    private static (int FP, int Stamina) CapturePools(SavedCharacter character) =>
        (character.Record.FP, character.Record.Stamina);

    /// <summary>Gives back any FP or STM lost to an intermediate cap, never above the final maximum.</summary>
    private static void RestorePools(SavedCharacter character, (int FP, int Stamina) pools)
    {
        if (!character.AppliesRecordEffects)
            return;
        character.Record.FP = Math.Min(pools.FP, SavedEquipmentStats.GetMaxFP(character));
        character.Record.Stamina = Math.Min(pools.Stamina, SavedEquipmentStats.GetMaxStamina(character));
    }
}
