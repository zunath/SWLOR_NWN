using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>Corrects wearable types in place while preserving properties, ownership and item identities.</summary>
public static class EquipmentTypeMigration
{
    public static IReadOnlyDictionary<string, (BaseItem LegacyType, BaseItem Type, int Model, InventorySlot Slot)> Corrections { get; } =
        new Dictionary<string, (BaseItem, BaseItem, int, InventorySlot)>(StringComparer.OrdinalIgnoreCase)
        {
            ["absdef_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["adamguard_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["alpharhy_l1"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["apexbite_l1"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["darkhung_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["deadhand_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["deccommand_l1"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["eclipse_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["embunker_l1"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["emcocktail_l1"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["forcebane_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["forcebeast_l1"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["guardmst_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["holdline_l1"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["infconduit_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["invinc_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["killbeacon_l1"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["killbox_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["lastword_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["lightstand_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["oneshot_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["overbarr_l1"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["perflurry_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["primover_l1"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["rainsteel_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["sabcycl_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["sabstorm_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["scraplock_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["soulasc_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["sysshut_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["thermdet_l1"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["unbrbeast_l1"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["unmovctr_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["untinst_l1"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["vitrupt_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["worldbrk_l3"] = (BaseItem.Gloves, BaseItem.Helmet, 33, InventorySlot.Head),
            ["hexweave"] = (BaseItem.Bracer, BaseItem.Cloak, 117, InventorySlot.Cloak),
            ["pil_nulaaglove"] = (BaseItem.Bracer, BaseItem.Gloves, 126, InventorySlot.Arms),
            ["reclaim_gaunt"] = (BaseItem.Bracer, BaseItem.Gloves, 126, InventorySlot.Arms),
            ["brawl_wraps"] = (BaseItem.Bracer, BaseItem.Gloves, 126, InventorySlot.Arms),
            ["deton_cestus"] = (BaseItem.Bracer, BaseItem.Gloves, 126, InventorySlot.Arms),
            ["piston_gaunt"] = (BaseItem.Armor, BaseItem.Gloves, 126, InventorySlot.Arms),
            ["fang_gaunt"] = (BaseItem.Bracer, BaseItem.Gloves, 176, InventorySlot.Arms),
            ["tarnapexmawcr"] = (BaseItem.Bracer, BaseItem.Gloves, 176, InventorySlot.Arms),
            ["bunkerbreakcr"] = (BaseItem.Bracer, BaseItem.Gloves, 176, InventorySlot.Arms),
            ["canyonbulwrkcr"] = (BaseItem.Bracer, BaseItem.Gloves, 176, InventorySlot.Arms),
            ["cryptwardencr"] = (BaseItem.Bracer, BaseItem.Gloves, 176, InventorySlot.Arms),
            ["detonite_knuck"] = (BaseItem.Bracer, BaseItem.Gloves, 176, InventorySlot.Arms),
            ["flurrychampcr"] = (BaseItem.Bracer, BaseItem.Gloves, 176, InventorySlot.Arms),
            ["forgewrightcr"] = (BaseItem.Bracer, BaseItem.Gloves, 176, InventorySlot.Arms),
            ["invictuscr"] = (BaseItem.Bracer, BaseItem.Gloves, 176, InventorySlot.Arms),
            ["pit_cestus"] = (BaseItem.Bracer, BaseItem.Gloves, 176, InventorySlot.Arms),
            ["sabraetrialcr"] = (BaseItem.Bracer, BaseItem.Gloves, 176, InventorySlot.Arms),
            ["triagewardencr"] = (BaseItem.Bracer, BaseItem.Gloves, 176, InventorySlot.Arms),
        };

    public static bool IsLegacyEquipment(string resref, BaseItem type) =>
        resref != null && Corrections.TryGetValue(resref, out var correction) && correction.LegacyType == type;

    internal static bool ConvertItem(uint item)
    {
        if (!GetIsObjectValid(item) || !IsLegacyEquipment(GetResRef(item), GetBaseItemType(item)))
            return false;
        var correction = Corrections[GetResRef(item)];
        var owner = GetItemPossessor(item);
        var previousSlot = GetLegacySlot(correction.LegacyType);
        var equipped = GetObjectType(owner) == ObjectType.Creature && GetItemInSlot(previousSlot, owner) == item;
        var record = GetIsPC(owner) && !GetIsDM(owner) && !GetIsDMPossessed(owner) ? DB.Get<Player>(GetObjectUUID(owner)) : null;
        var hp = equipped ? ObjectPlugin.GetCurrentHitPoints(owner) : 0;
        var fp = equipped ? Stat.GetCurrentFP(owner, record) : 0;
        var stamina = equipped ? Stat.GetCurrentStamina(owner, record) : 0;
        try
        {
            if (equipped && !PlayerEquipmentStorage.TryUnequip(owner, item, previousSlot))
                throw new PlayerMigrationDeferredException("Free inventory space, then reconnect to complete your equipment update.");
            ItemPlugin.SetBaseItemType(item, correction.Type);
            ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, 0, correction.Model);
            if (equipped && !GetIsObjectValid(GetItemInSlot(correction.Slot, owner)))
                CreaturePlugin.RunEquip(owner, item, correction.Slot);
            if (equipped && GetItemPossessor(item) != owner)
                throw new InvalidOperationException("Corrected equipment was not retained by its owner.");
        }
        finally
        {
            if (equipped)
            {
                SetCurrentHitPoints(owner, Math.Min(hp, GetMaxHitPoints(owner)));
                if (record != null)
                {
                    record = DB.Get<Player>(GetObjectUUID(owner));
                    record.FP = Math.Min(fp, Stat.GetMaxFP(owner, record));
                    record.Stamina = Math.Min(stamina, Stat.GetMaxStamina(owner, record));
                    DB.Set(record);
                }
                else
                {
                    SetLocalInt(owner, "FP", Math.Min(fp, Stat.GetMaxFP(owner)));
                    SetLocalInt(owner, "STAMINA", Math.Min(stamina, Stat.GetMaxStamina(owner)));
                }
                Stat.RefreshCombatReadinessEquipment(owner);
            }
        }
        return true;
    }

    internal static InventorySlot GetLegacySlot(BaseItem type) => type == BaseItem.Armor ? InventorySlot.Chest : InventorySlot.Arms;
}
