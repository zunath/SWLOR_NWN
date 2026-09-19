using System.Collections.Generic;
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
        baseItem == BaseItem.Longsword && (string.Equals(resref, "longsword_b", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(resref, "b_longsword", StringComparison.OrdinalIgnoreCase));

    public static bool Normalize(uint item)
    {
        if (!GetIsObjectValid(item) || !IsBasicVibroblade(GetBaseItemType(item), GetResRef(item))) return false;
        var changed = false;
        // Only replace the default name. Preserve all deliberate player/DM custom names.
        if (GetName(item) == "Basic Longsword")
        {
            SetName(item, DisplayName);
            changed = true;
        }
        var properties = new HashSet<ItemPropertyType>();
        for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            properties.Add(GetItemPropertyType(ip));
        // The retired longsword_b relied on NWN enhancement damage and can survive with
        // no SWLOR DMG at all. Restore missing fields to the canonical basic template;
        // existing damage, enhancements, damage types, appearance and identity are kept.
        void AddMissing(ItemPropertyType type, int subtype, int value)
        {
            if (properties.Contains(type)) return;
            MigrationObject.AddProperty(item, ItemPropertyCustom(type, subtype, value), AddItemPropertyPolicy.IgnoreExisting);
            changed = true;
        }
        AddMissing(ItemPropertyType.DMG, -1, 5);
        AddMissing(ItemPropertyType.Delay, -1, 23);
        AddMissing(ItemPropertyType.RequiresSkill, (int)SkillType.Vibroblade, 0);
        return changed;
    }
}
