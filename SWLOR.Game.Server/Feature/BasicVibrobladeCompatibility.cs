using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature;

/// <summary>Repairs the retired basic vibroblade template without replacing player-owned items.</summary>
public static class BasicVibrobladeCompatibility
{
    public const string DisplayName = "Basic Vibroblade LS";

    private sealed record BasicWeaponProfile(
        BaseItem BaseItem,
        string ResRef,
        SkillType Skill,
        int Damage,
        int Delay,
        string DefaultName = null,
        string DisplayName = null);

    private static readonly BasicWeaponProfile[] Profiles =
    {
        new(BaseItem.Longsword, "longsword_b", SkillType.Vibroblade, 5, 23, "Basic Longsword", DisplayName),
        new(BaseItem.Longsword, "b_longsword", SkillType.Vibroblade, 5, 23),
        new(BaseItem.Dagger, "dagger_b", SkillType.Vibroknife, 5, 22),
        new(BaseItem.Dagger, "b_knife", SkillType.Vibroknife, 5, 22),
        new(BaseItem.DoubleAxe, "doubleaxe_b", SkillType.TwinBlade, 5, 23),
        new(BaseItem.TwoBladedSword, "twinblade_b", SkillType.TwinBlade, 5, 23),
    };

    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void OnEnter()
    {
        var player = GetEnteringObject();
        if (GetIsPC(player)) NormalizeInventory(player);
    }

    [NWNEventHandler(ScriptName.OnModuleAcquire)]
    public static void OnAcquire() => NormalizeInventory(GetModuleItemAcquired());

    public static bool NormalizeInventory(uint obj)
    {
        var visited = new HashSet<uint>();
        bool Visit(uint current)
        {
            if (!GetIsObjectValid(current) || !visited.Add(current)) return false;
            var changed = false;
            if (GetObjectType(current) == ObjectType.Item) changed |= Normalize(current);
            if (GetObjectType(current) == ObjectType.Creature)
                for (var slot = 0; slot < NumberOfInventorySlots; slot++)
                    changed |= Visit(GetItemInSlot((InventorySlot)slot, current));
            if (GetHasInventory(current))
                for (var item = GetFirstItemInInventory(current); GetIsObjectValid(item); item = GetNextItemInInventory(current))
                    changed |= Visit(item);
            return changed;
        }
        return Visit(obj);
    }

    public static bool IsBasicVibroblade(BaseItem baseItem, string resref) =>
        TryGetProfile(baseItem, resref, out var profile) && profile.Skill == SkillType.Vibroblade;

    public static bool IsBasicVibroknife(BaseItem baseItem, string resref) =>
        TryGetProfile(baseItem, resref, out var profile) && profile.Skill == SkillType.Vibroknife;

    public static IReadOnlyList<(ItemPropertyType Type, int Subtype, int Value)> GetMissingProperties(
        BaseItem baseItem,
        string resref,
        IEnumerable<ItemPropertyType> existingProperties)
    {
        if (!TryGetProfile(baseItem, resref, out var profile))
            return Array.Empty<(ItemPropertyType, int, int)>();

        var existing = new HashSet<ItemPropertyType>(existingProperties);
        var missing = new List<(ItemPropertyType Type, int Subtype, int Value)>();
        if (!existing.Contains(ItemPropertyType.DMG))
            missing.Add((ItemPropertyType.DMG, -1, profile.Damage));
        if (!existing.Contains(ItemPropertyType.Delay))
            missing.Add((ItemPropertyType.Delay, -1, profile.Delay));
        if (!existing.Contains(ItemPropertyType.RequiresSkill))
            missing.Add((ItemPropertyType.RequiresSkill, (int)profile.Skill, 0));
        return missing;
    }

    public static bool Normalize(uint item)
    {
        if (!GetIsObjectValid(item) || !TryGetProfile(GetBaseItemType(item), GetResRef(item), out var profile)) return false;
        var changed = false;
        // Only replace the default name. Preserve all deliberate player/DM custom names.
        if (profile.DefaultName != null && GetName(item) == profile.DefaultName)
        {
            SetName(item, profile.DisplayName);
            changed = true;
        }
        var properties = new HashSet<ItemPropertyType>();
        for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            properties.Add(GetItemPropertyType(ip));
        // Retired basic templates can survive without SWLOR DMG. Restore missing
        // fields while preserving existing properties, appearance, identity and local data.
        foreach (var property in GetMissingProperties(GetBaseItemType(item), GetResRef(item), properties))
        {
            MigrationObject.AddProperty(item,
                ItemPropertyCustom(property.Type, property.Subtype, property.Value),
                AddItemPropertyPolicy.IgnoreExisting);
            changed = true;
        }
        return changed;
    }

    private static bool TryGetProfile(BaseItem baseItem, string resref, out BasicWeaponProfile profile)
    {
        profile = Profiles.FirstOrDefault(candidate =>
            candidate.BaseItem == baseItem && string.Equals(candidate.ResRef, resref, StringComparison.OrdinalIgnoreCase));
        return profile != null;
    }
}
