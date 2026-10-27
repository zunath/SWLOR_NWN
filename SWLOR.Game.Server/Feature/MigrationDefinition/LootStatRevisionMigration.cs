using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Moves loot created before the loot stat revision onto the revised blueprint values. Each revised
/// property shifts by the blueprint difference, so crafted enhancements merged into it are kept.
/// Revised blueprints carry the revision local, which also marks every migrated item. Droids are covered
/// both as saved controller data and as summoned creatures, whose items are saved back onto the controller.
/// The same item visit repairs legacy headgear slots before the unreleased migration is checkpointed, and release
/// migrations (which also update artwork) add the droid weapon skills (<see cref="DroidWeaponSkillMigration"/>).
/// </summary>
public static class LootStatRevisionMigration
{
    public const string RevisionVariable = "ITEM_STATS_REVISION";
    public const int CurrentRevision = 1;
    private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";

    [NWNEventHandler(ScriptName.OnModuleAcquire)]
    public static void OnAcquire()
    {
        var item = GetModuleItemAcquired();
        var owner = GetItemPossessor(item);
        // Items loading with a character at login are not revised here: the boot-time character upgrade
        // already revised the saved items, and a revision marker set during that load does not persist,
        // so revising them here would repeat at every login.
        if (!GetIsObjectValid(GetArea(owner)))
            return;
        if (GetIsPC(owner) && !GetIsDM(owner) && !GetIsDMPossessed(owner))
            MigrateObject(item);
        else if (Droid.IsDroid(owner) && MigrateObject(item))
            Droid.SaveItemSnapshot(owner);
    }

    /// <summary>
    /// Revises a player's equipped and carried items and their summoned droid, a summoned droid,
    /// an item, or a container and its contents.
    /// </summary>
    public static bool MigrateObject(uint obj) => MigrateObject(obj, new HashSet<uint>(), true, true);

    public static bool MigratePlayerItems(uint player) => MigrateObject(player, new HashSet<uint>(), true, true, true);

    internal static bool MigrateHeadgear(uint obj) => MigrateObject(obj, new HashSet<uint>(), false, true);

    private static bool MigrateObject(uint obj, HashSet<uint> visited, bool reviseStats, bool repairHeadgear, bool updateArtwork = false)
    {
        if (!GetIsObjectValid(obj) || !visited.Add(obj))
            return false;

        var objectType = GetObjectType(obj);
        var pools = (HP: 0, FP: 0, Stamina: 0);
        var changed = false;
        if (objectType == ObjectType.Creature)
        {
            // Stored DM creatures, DM-possessed creatures and live NPCs keep their authored gear.
            if (!Droid.IsDroid(obj) && (!GetIsPC(obj) || GetIsDM(obj) || GetIsDMPossessed(obj)))
                return false;
            pools = CapturePools(obj);
            // Establish safe storage before revising any equipped properties.
            // A full inventory defers migration without leaving DB-only deltas.
            if (repairHeadgear)
                changed |= HeadgearSlotMigration.ConvertItem(GetItemInSlot(InventorySlot.Chest, obj));
            for (var slot = 0; slot < NumberOfInventorySlots; slot++)
                changed |= MigrateObject(GetItemInSlot((InventorySlot)slot, obj), visited, reviseStats, repairHeadgear, updateArtwork);
            if (GetIsPC(obj))
                changed |= MigrateObject(Droid.GetDroid(obj), visited, reviseStats, repairHeadgear, updateArtwork);
        }
        else if (objectType == ObjectType.Item)
        {
            // Direct acquisition follows the same ordering: storage failure must
            // precede any property revision or persisted wearer-stat difference.
            if (repairHeadgear)
            {
                changed |= HeadgearSlotMigration.ConvertItem(obj);
                changed |= EquipmentTypeMigration.ConvertItem(obj);
            }
            if (updateArtwork) changed |= ItemIconMigration.MigrateItem(obj);
            if (reviseStats) changed |= MigrateItem(obj);
            // Release migrations only: on acquisition it would copy boosts onto parts made after the release.
            if (updateArtwork) changed |= DroidWeaponSkillMigration.MigrateItem(obj);
            changed |= MigrateObject(GetSummonedDroid(obj), visited, reviseStats, repairHeadgear, updateArtwork);
            var original = GetLocalString(obj, ConstructedDroidVariable);
            var migrated = HeadgearSlotMigration.MigrateDroid(original, data =>
                MigrateSerializedObject(data, out var result, reviseStats, repairHeadgear, updateArtwork) ? result : data, repairHeadgear);
            if (original != migrated)
            {
                SetLocalString(obj, ConstructedDroidVariable, migrated);
                changed = true;
            }
        }
        if (GetHasInventory(obj))
        {
            // Conversion can stow equipment in a recovery bag. Snapshot before
            // recursing and visit each native item once, regardless of its slot.
            var contents = new List<uint>();
            for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item); item = GetNextItemInInventory(obj))
                contents.Add(item);
            foreach (var item in contents)
                changed |= MigrateObject(item, visited, reviseStats, repairHeadgear, updateArtwork);
        }
        if (changed && objectType == ObjectType.Creature)
        {
            RestorePools(obj, pools);
            if (Droid.IsDroid(obj)) Droid.SaveItemSnapshot(obj);
        }
        return changed;
    }

    /// <summary>
    /// Records current HP, FP and STM before equipped items are revised. Each item's difference is applied
    /// on its own, so an item that lowers a maximum can clamp a pool that a later item raises again.
    /// </summary>
    private static (int HP, int FP, int Stamina) CapturePools(uint creature)
    {
        var dbPlayer = GetIsPC(creature) ? DB.Get<Player>(GetObjectUUID(creature)) : null;
        return (ObjectPlugin.GetCurrentHitPoints(creature), Stat.GetCurrentFP(creature, dbPlayer), Stat.GetCurrentStamina(creature, dbPlayer));
    }

    /// <summary>Gives back any HP, FP or STM lost to an intermediate cap, never above the final maximum.</summary>
    private static void RestorePools(uint creature, (int HP, int FP, int Stamina) pools)
    {
        SetCurrentHitPoints(creature, Math.Min(pools.HP, GetMaxHitPoints(creature)));

        if (!GetIsPC(creature))
        {
            SetLocalInt(creature, "FP", Math.Min(pools.FP, Stat.GetMaxFP(creature)));
            SetLocalInt(creature, "STAMINA", Math.Min(pools.Stamina, Stat.GetMaxStamina(creature)));
            return;
        }
        var dbPlayer = DB.Get<Player>(GetObjectUUID(creature));
        if (dbPlayer == null)
            return;
        dbPlayer.FP = Math.Min(pools.FP, Stat.GetMaxFP(creature, dbPlayer));
        dbPlayer.Stamina = Math.Min(pools.Stamina, Stat.GetMaxStamina(creature, dbPlayer));
        DB.Set(dbPlayer);
    }

    /// <summary>Preserves saved identities and unchanged payloads, releasing every temporary native object.</summary>
    public static bool MigrateSerializedObject(string original, out string migrated)
        => MigrateSerializedObject(original, out migrated, true, true);

    internal static bool MigrateStoredObjectData(string original, out string migrated, bool repairHeadgear = true, Action<uint> rootChanged = null)
        => MigrateSerializedObject(original, out migrated, true, repairHeadgear, true, rootChanged);

    private static bool MigrateSerializedObject(string original, out string migrated, bool reviseStats, bool repairHeadgear, bool updateArtwork = false, Action<uint> rootChanged = null)
    {
        migrated = original;
        if (string.IsNullOrWhiteSpace(original))
            return false;
        if (StoredObjectData.IsCreatureData(original))
            return updateArtwork && ItemIconMigration.MigrateSerializedObject(original, out migrated);
        var inventory = StoredObjectData.ReadInventory(original);
        var obj = MigrationObject.Deserialize(inventory?.PrepareForNativeLoad(null) ?? original);
        try
        {
            var refreshRootMetadata = repairHeadgear && GetObjectType(obj) == ObjectType.Item &&
                (HeadgearSlotMigration.IsLegacyHeadgear(GetResRef(obj), GetBaseItemType(obj)) ||
                 HeadgearSlotMigration.GetCorrectedModel(GetResRef(obj), GetBaseItemType(obj),
                     GetItemAppearance(obj, ItemAppearanceType.SimpleModel, 0)) != GetItemAppearance(obj, ItemAppearanceType.SimpleModel, 0) ||
                 EquipmentTypeMigration.IsLegacyEquipment(GetResRef(obj), GetBaseItemType(obj))) ||
                updateArtwork && ItemAppearanceCatalogMigration.IsLegacyAppearance(obj);
            if (!MigrateObject(obj, new HashSet<uint>(), reviseStats, repairHeadgear, updateArtwork))
                return false;
            if (refreshRootMetadata) rootChanged?.Invoke(obj);
            migrated = MigrationObject.Serialize(obj, original);
            if (inventory != null)
                migrated = inventory.CopyMigratedInventory(migrated);
            return true;
        }
        finally
        {
            MigrationObject.DestroyTemporaryObject(obj);
        }
    }

    private static bool MigrateItem(uint item)
    {
        var alreadyRevisedRare = GetLocalInt(item, RareLootStatRevisionCatalog.RevisionVariable) >= RareLootStatRevisionCatalog.CurrentRevision;
        var changed = ApplyRevision(item, LootStatRevisionCatalog.Changes, RevisionVariable, CurrentRevision);
        changed |= ApplyRevision(item, RareLootStatRevisionCatalog.Changes,
            RareLootStatRevisionCatalog.RevisionVariable, RareLootStatRevisionCatalog.CurrentRevision);
        if (alreadyRevisedRare)
            changed |= ApplyRevision(item, EquipmentTypeStatRevisionCatalog.Changes,
                EquipmentTypeStatRevisionCatalog.RevisionVariable, EquipmentTypeStatRevisionCatalog.CurrentRevision);
        else if (EquipmentTypeStatRevisionCatalog.Changes.ContainsKey(GetResRef(item)) &&
                 GetLocalInt(item, EquipmentTypeStatRevisionCatalog.RevisionVariable) < EquipmentTypeStatRevisionCatalog.CurrentRevision)
        {
            // The revised rare catalog already applied the final cloak value.
            SetLocalInt(item, EquipmentTypeStatRevisionCatalog.RevisionVariable, EquipmentTypeStatRevisionCatalog.CurrentRevision);
            changed = true;
        }
        changed |= ApplyRevision(item, FoundryLootStatRevisionCatalog.Changes,
            FoundryLootStatRevisionCatalog.RevisionVariable, FoundryLootStatRevisionCatalog.CurrentRevision);
        changed |= ApplyRevision(item, ArkanianArmorStatRevisionCatalog.Changes,
            ArkanianArmorStatRevisionCatalog.RevisionVariable, ArkanianArmorStatRevisionCatalog.CurrentRevision,
            current => ReviseIceProtection(current, ArkanianArmorStatRevisionCatalog.IceResistance));
        changed |= ApplyRevision(item, RetiredDefenseStatRevisionCatalog.Changes,
            RetiredDefenseStatRevisionCatalog.RevisionVariable, RetiredDefenseStatRevisionCatalog.CurrentRevision,
            ReviseRetiredDefense);
        return changed;
    }

    private static IEnumerable<LootStatChange> ReviseRetiredDefense(uint item) =>
        RetiredDefenseStatRevisionCatalog.ConvertedResistanceChanges(GetResRef(item),
            subtype => ReadTotal(item, ItemPropertyType.Resistance, subtype).Total).ToArray();

    private static bool ApplyRevision(uint item, IReadOnlyDictionary<string, LootStatChange[]> catalog,
        string revisionVariable, int revision, Func<uint, IEnumerable<LootStatChange>> itemChanges = null)
    {
        if (!catalog.TryGetValue(GetResRef(item), out var catalogChanges) ||
            GetLocalInt(item, revisionVariable) >= revision)
            return false;
        var changes = itemChanges == null ? catalogChanges : catalogChanges.Concat(itemChanges(item)).ToArray();

        // An equipped item's stats are already counted on its wearer (a player's record or a droid's
        // skin). Apply only each stat's difference there: removing every old stat first would clamp
        // current HP, FP and STM to a temporarily lower maximum.
        var wearer = GetWearer(item);
        foreach (var change in changes)
        {
            var difference = ApplyChange(item, change);
            if (difference != 0 && GetIsObjectValid(wearer) && CountsOnWearer(change))
                EquipmentStats.ApplyStatAmount(wearer, item, change.Type, PropertySubtype(change),
                    Math.Abs(difference), difference > 0);
        }
        SetLocalInt(item, revisionVariable, revision);

        if (GetIsObjectValid(wearer))
            Stat.RefreshCombatReadinessEquipment(wearer);
        return true;
    }

    /// <summary>
    /// The combat replacement already turned older copies' Ice Defense into at least the base Ice Resistance.
    /// Later copies carry the old blueprint's retired Ice Defense, or have lost it, so their Ice Resistance
    /// holds only enhancements; the base adds to it as on a new copy.
    /// </summary>
    private static IEnumerable<LootStatChange> ReviseIceProtection(uint item, int baseResistance)
    {
        var defense = ReadTotal(item, ItemPropertyType.Defense, (int)CombatDamageType.Ice).Total;
        var resistance = ReadTotal(item, ItemPropertyType.Resistance, (int)ResistanceType.Ice).Total;
        if (defense > 0)
            yield return new LootStatChange(ItemPropertyType.Defense, (int)CombatDamageType.Ice, defense, 0);
        var missingBase = defense > 0 ? defense : resistance < baseResistance ? baseResistance : 0;
        yield return new LootStatChange(ItemPropertyType.Resistance, (int)ResistanceType.Ice, resistance, resistance + missingBase);
    }

    /// <summary>Shifts one property by the blueprint difference and returns how much the item's total changed.</summary>
    private static int ApplyChange(uint item, LootStatChange change)
    {
        var (total, matches) = ReadTotal(item, change.Type, change.Subtype);
        var revised = total - change.Previous + change.Current;
        if (revised < 0)
            revised = 0;
        if (revised == total && matches.Count <= 1)
            return 0;

        foreach (var ip in matches)
            MigrationObject.RemoveProperty(item, ip);
        var costTableValue = change.Type == ItemPropertyType.Resistance ? Resistance.EncodeItemPropertyCostTableValue(revised) : revised;
        if (revised > 0)
            MigrationObject.AddProperty(item, ItemPropertyCustom(change.Type, PropertySubtype(change), costTableValue), AddItemPropertyPolicy.IgnoreExisting);
        return revised - total;
    }

    private static (int Total, List<ItemProperty> Matches) ReadTotal(uint item, ItemPropertyType type, int subtype)
    {
        // Collect first: removing a property mid-iteration skips the next one.
        var matches = new List<ItemProperty>();
        var total = 0;
        for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
        {
            if (GetItemPropertyType(ip) != type ||
                GetItemPropertyDurationType(ip) != DurationType.Permanent ||
                HasSubtype(type) && GetItemPropertySubType(ip) != subtype)
                continue;
            total += type == ItemPropertyType.Resistance
                ? Resistance.DecodeItemPropertyCostTableValue(GetItemPropertyCostTableValue(ip))
                : GetItemPropertyCostTableValue(ip);
            matches.Add(ip);
        }
        return (total, matches);
    }

    // Retired elemental Defense never counted on the wearer, and its property cannot be rebuilt.
    private static bool CountsOnWearer(LootStatChange change) =>
        change.Type != ItemPropertyType.Defense || ((CombatDamageType)change.Subtype).IsDefenseDamageType();

    private static bool HasSubtype(ItemPropertyType type) =>
        type is ItemPropertyType.Defense or ItemPropertyType.Resistance or ItemPropertyType.RequiresSkill;

    private static int PropertySubtype(LootStatChange change) => HasSubtype(change.Type) ? change.Subtype : -1;

    private static uint GetWearer(uint item)
    {
        var possessor = GetItemPossessor(item);
        var isPlayer = GetIsPC(possessor) && !GetIsDM(possessor) && !GetIsDMPossessed(possessor);
        if (!isPlayer && !Droid.IsDroid(possessor))
            return OBJECT_INVALID;
        for (var slot = 0; slot < NumberOfInventorySlots; slot++)
            if (GetItemInSlot((InventorySlot)slot, possessor) == item)
                return possessor;
        return OBJECT_INVALID;
    }

    private static uint GetSummonedDroid(uint controller)
    {
        var droid = Droid.GetDroid(GetItemPossessor(controller));
        return GetIsObjectValid(droid) && Droid.GetControllerItem(droid) == controller ? droid : OBJECT_INVALID;
    }

}
