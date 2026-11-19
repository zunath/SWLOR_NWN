using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{

    internal static void StampLegacyAppearance(EngineTestContext ctx, uint item)
    {
        var revision = ItemAppearanceCatalogMigration.Revisions[GetResRef(item)];
        // A later release's revision (GaderffiiAppearanceMigration) moves a blueprint on from where this one ended.
        var blueprint = GaderffiiAppearanceMigration.Revisions.TryGetValue(GetResRef(item), out var later) &&
                        later.Previous.SequenceEqual(revision.Current)
            ? later.Current
            : revision.Current;
        ctx.AssertEqual(string.Join(",", blueprint), string.Join(",", AppearanceParts(item)),
            $"{GetResRef(item)} blueprint reads as its catalog appearance");
        for (var index = 0; index < revision.Previous.Length; index++)
            switch (revision.Previous.Length)
            {
                case 3:
                    ItemPlugin.SetItemAppearance(item, ItemAppearanceType.WeaponModel, index, revision.Previous[index] / 10);
                    ItemPlugin.SetItemAppearance(item, ItemAppearanceType.WeaponColor, index, revision.Previous[index] % 10);
                    break;
                case (int)AppearanceArmor.Num:
                    ItemPlugin.SetItemAppearance(item, ItemAppearanceType.ArmorModel, index, revision.Previous[index]);
                    break;
                default:
                    ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, index, revision.Previous[index]);
                    break;
            }
        ctx.AssertEqual(string.Join(",", revision.Previous), string.Join(",", AppearanceParts(item)),
            $"{GetResRef(item)} fixture carries the shipped appearance");
    }

    private static int[] AppearanceParts(uint item) =>
        (int[])Invoke("ItemAppearanceCatalogMigration", "ReadParts", item, ItemAppearanceCatalogMigration.Revisions[GetResRef(item)].Current.Length);

    private static void StampLegacyArkanianArmor(uint item, Dictionary<(ItemPropertyType, int), int> enhancements,
        int iceDefense, int iceResistance)
    {
        var shipped = ArkanianArmorStatRevisionCatalog.Changes[GetResRef(item)]
            .Select(change => (change.Type, change.Subtype, change.Previous))
            .Append((ItemPropertyType.Defense, (int)CombatDamageType.Ice, 0))
            .Append((ItemPropertyType.Resistance, (int)ResistanceType.Ice, 0));
        foreach (var (type, subtype, previous) in shipped)
        {
            var matches = new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                if (GetItemPropertyType(ip) == type && (!HasArkanianSubtype(type) || GetItemPropertySubType(ip) == subtype))
                    matches.Add(ip);
            foreach (var ip in matches)
                Invoke("MigrationObject", "RemoveProperty", item, ip);
            var legacy = previous + enhancements.GetValueOrDefault((type, subtype));
            if (legacy > 0)
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(type, HasArkanianSubtype(type) ? subtype : -1, legacy), item);
        }
        // Current constructors reject the retired Ice Defense subtype, so it is written natively.
        if (iceDefense > 0)
            AddLegacyProperty(item, ItemPropertyType.Defense, (int)CombatDamageType.Ice, 35, iceDefense);
        if (iceResistance > 0)
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Resistance, (int)ResistanceType.Ice,
                Resistance.EncodeItemPropertyCostTableValue(iceResistance)), item);
        DeleteLocalInt(item, ArkanianArmorStatRevisionCatalog.RevisionVariable);
    }

    private static bool HasArkanianSubtype(ItemPropertyType type) =>
        type is ItemPropertyType.Defense or ItemPropertyType.Resistance or ItemPropertyType.RequiresSkill;

    private static (int MaxHP, int Physical, int Force, int IceDefense, int IceResistance, int StaminaRegen) ReadArmorTotals(uint player) =>
        ReadArmorTotals(DB.Get<Player>(GetObjectUUID(player)));

    private static (int MaxHP, int Physical, int Force, int IceDefense, int IceResistance, int StaminaRegen) ReadArmorTotals(Player record)
    {
        return (record.MaxHP, record.Defenses.GetValueOrDefault(CombatDamageType.Physical), record.Defenses.GetValueOrDefault(CombatDamageType.Force),
            record.Defenses.GetValueOrDefault(CombatDamageType.Ice), record.Resistances.GetValueOrDefault(ResistanceType.Ice), record.STMRegen);
    }

    private static IEnumerable<uint> Carried(uint creature)
    {
        for (var item = GetFirstItemInInventory(creature); GetIsObjectValid(item); item = GetNextItemInInventory(creature))
            yield return item;
    }
}
