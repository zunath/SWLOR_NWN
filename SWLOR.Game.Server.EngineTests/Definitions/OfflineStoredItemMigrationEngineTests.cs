using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    private const string OfflineMigrationCategory = "OfflineMigration";
    private const string CurrentLootLabel = "already migrated loot";
    private static readonly Assembly OfflineMigrationAssembly = typeof(EquipmentTypeMigration).Assembly;

    /// <summary>
    /// The offline sweep must save what the engine sweep saves. Both converters run on the same legacy payloads,
    /// both results load into the engine, and everything the engine reads back is compared. The production-copy
    /// parity check holds few of these legacy kinds, so this covers each rule the offline sweep mirrors.
    /// </summary>
    [EngineTest("Offline stored-item sweep saves what the engine sweep saves for every legacy item kind", Category = OfflineMigrationCategory, TimeoutSeconds = 240f)]
    public static async Task OfflineStoredItemSweepMatchesEngine(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        var savedCreature = ctx.SpawnCreature("civilian", 2f);
        await ctx.WaitFrameAsync();
        var fixtures = new List<(string Label, string Data, bool RepairHeadgear)>
        {
            ("legacy Ruptor visor", LegacyHeadgearFixtures.Ruptor, true),
            ("legacy Thermal visor", LegacyHeadgearFixtures.Thermal, true),
            ("Ruptor visor outfit template", LegacyHeadgearFixtures.Ruptor, false),
        };

        foreach (var (resref, correction) in EquipmentTypeMigration.Corrections)
        {
            var item = await CreateItemAsync(ctx, owner, resref, owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ItemPlugin.SetBaseItemType(item, correction.LegacyType);
                ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, 0, correction.LegacyType == BaseItem.Gloves ? 198 : 176);
                if (LootStatRevisionCatalog.Changes.ContainsKey(resref))
                    StampLegacyLoot(item, NoEnhancements);
                fixtures.Add(($"legacy {resref}", ObjectPlugin.Serialize(item), true));
                DestroyObject(item);
            });
        }

        var gloves = await CreateItemAsync(ctx, owner, RevisedGloves, owner);
        var buffedGloves = await CreateItemAsync(ctx, owner, RevisedGloves, owner);
        var artwork = await CreateItemAsync(ctx, owner, "aen_hp1", owner);
        var current = await CreateItemAsync(ctx, owner, RevisedPistol, owner);
        var bag = await CreateItemAsync(ctx, owner, "bag_sm", owner);
        var nestedGloves = await CreateItemAsync(ctx, owner, RevisedGloves, bag);
        var nestedArtwork = await CreateItemAsync(ctx, owner, "aen_hp1", bag);
        var controller = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, owner);
        var creatureGloves = await CreateItemAsync(ctx, savedCreature, RevisedGloves, savedCreature);
        var creatureArtwork = await CreateItemAsync(ctx, savedCreature, "aen_hp1", savedCreature);
        var creaturePistol = await CreateItemAsync(ctx, savedCreature, "b_pistol", savedCreature);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            StampLegacyLoot(gloves, GloveEnhancements);
            // A temporary bonus of a revised stat must stay temporary and outside the revision.
            StampLegacyLoot(buffedGloves, GloveEnhancements);
            AddItemProperty(DurationType.Temporary, ItemPropertyCustom(ItemPropertyType.Attack, -1, 3), buffedGloves, 3600f);
            ItemPlugin.SetItemAppearance(artwork, ItemAppearanceType.SimpleModel, 0, 88);
            SetLocalInt(artwork, "CRAFTED_QUALITY", 29);
            StampLegacyLoot(nestedGloves, GloveEnhancements);
            ItemPlugin.SetItemAppearance(nestedArtwork, ItemAppearanceType.SimpleModel, 0, 88);
            StampLegacyLoot(creatureGloves, GloveEnhancements);
            ItemPlugin.SetItemAppearance(creatureArtwork, ItemAppearanceType.SimpleModel, 0, 88);
            // A pistol saved with arrow ammunition from its legacy bow base item. The engine path re-saves
            // creature items after loading them, so loading the offline result must yield the same pistol.
            ItemPlugin.SetBaseItemType(creaturePistol, BaseItem.Pistol);
            RemoveMatching(creaturePistol, ItemPropertyType.UnlimitedAmmunition, 0);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.UnlimitedAmmunition, 0, 1), creaturePistol);
            ItemPlugin.SetBaseItemType(creaturePistol, BaseItem.Sling);
            var legacyGloves = ObjectPlugin.Serialize(gloves);
            SetLocalString(controller, "CONSTRUCTED_DROID", JsonConvert.SerializeObject(new ConstructedDroid
            {
                SerializedHead = ObjectPlugin.Serialize(artwork),
                EquippedItems = { [InventorySlot.Arms] = legacyGloves },
                Inventory = { ["spare-visor"] = LegacyHeadgearFixtures.Thermal, ["spare-gloves"] = legacyGloves },
            }));
            fixtures.Add(("legacy loot with crafted enhancements", legacyGloves, true));
            var buffedData = ObjectPlugin.Serialize(buffedGloves);
            ctx.Log("Temporary bonus fixture: " + buffedData);
            fixtures.Add(("legacy loot with a temporary Attack bonus", buffedData, true));
            fixtures.Add(("legacy artwork", ObjectPlugin.Serialize(artwork), true));
            fixtures.Add((CurrentLootLabel, ObjectPlugin.Serialize(current), true));
            fixtures.Add(("container with legacy contents", ObjectPlugin.Serialize(bag), true));
            fixtures.Add(("droid controller with legacy parts", ObjectPlugin.Serialize(controller), true));
            fixtures.Add(("saved creature carrying legacy items", ObjectPlugin.Serialize(savedCreature), true));
        });

        foreach (var (label, data, repairHeadgear) in fixtures)
        {
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var engine = ConvertStoredItem("EngineStoredItemConverter", data, repairHeadgear);
                var offline = ConvertStoredItem("OfflineStoredItemConverter", data, repairHeadgear);
                if (label.StartsWith("saved creature"))
                    ctx.Log($"{label} results: engine {engine.Migrated} offline {offline.Migrated}");
                ctx.AssertEqual(label != CurrentLootLabel, engine.Changed, $"{label}: the fixture is legacy data the engine sweep changes");
                ctx.AssertEqual(engine.Changed, offline.Changed, $"{label}: both sweeps agree whether it changes");
                ctx.AssertEqual(engine.Converted, offline.Converted, $"{label}: both sweeps report the same listing icon and market category");
                ctx.AssertEqual(DescribeStoredObject(ctx, engine.Migrated), DescribeStoredObject(ctx, offline.Migrated),
                    $"{label}: the engine reads the same object from both results");
                ctx.Assert(!ConvertStoredItem("EngineStoredItemConverter", offline.Migrated, repairHeadgear).Changed,
                    $"{label}: the engine sweep finds nothing left to change in the offline result");
                ctx.Assert(!ConvertStoredItem("OfflineStoredItemConverter", offline.Migrated, repairHeadgear).Changed,
                    $"{label}: the offline sweep is idempotent");
            });
            await ctx.WaitFrameAsync();
        }
    }

    private static (bool Changed, string Migrated, string Converted) ConvertStoredItem(string converterName, string data, bool repairHeadgear)
    {
        var converter = OfflineMigrationAssembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition." + converterName);
        var convertedType = OfflineMigrationAssembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.ConvertedEquipment");
        var converted = new List<object>();
        var capture = typeof(MigrationEngineTests).GetMethod(nameof(CaptureConvertedEquipment), BindingFlags.NonPublic | BindingFlags.Static)
            .MakeGenericMethod(convertedType).Invoke(null, new object[] { converted });
        var arguments = new[] { data, null, repairHeadgear, capture };
        var changed = (bool)converter.GetMethod("MigrateStoredObjectData").Invoke(converter.GetField("Instance").GetValue(null), arguments);
        return (changed, (string)arguments[1], string.Join("; ", converted));
    }

    private static Action<T> CaptureConvertedEquipment<T>(List<object> sink) => value => sink.Add(value);

    /// <summary>Loads saved data and describes what the engine reads, including nested and droid-saved items.</summary>
    private static string DescribeStoredObject(EngineTestContext ctx, string data)
    {
        var obj = Deserialize(ctx, data);
        try
        {
            return DescribeLoadedObject(ctx, obj);
        }
        finally
        {
            // Releases the copy's identities so the other result loads with the same saved UUIDs.
            Invoke("MigrationObject", "DestroyTemporaryObject", obj);
        }
    }

    private static string DescribeLoadedObject(EngineTestContext ctx, uint obj)
    {
        var parts = new List<string> { $"{GetObjectType(obj)} {GetResRef(obj)} tag={GetTag(obj)} name={GetName(obj)}" };
        if (GetObjectType(obj) == ObjectType.Item)
        {
            parts.Add($"base={GetBaseItemType(obj)} model={GetItemAppearance(obj, ItemAppearanceType.SimpleModel, 0)} " +
                $"stack={GetItemStackSize(obj)} charges={GetItemCharges(obj)} value={GetGoldPieceValue(obj)}");
            var properties = new List<string>();
            for (var ip = GetFirstItemProperty(obj); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(obj))
                properties.Add($"{GetItemPropertyType(ip)}:{GetItemPropertySubType(ip)}:{GetItemPropertyCostTable(ip)}:" +
                    $"{GetItemPropertyCostTableValue(ip)}:{GetItemPropertyParam1(ip)}:{GetItemPropertyParam1Value(ip)}:{GetItemPropertyDurationType(ip)}");
            parts.Add("properties=" + string.Join(",", properties.OrderBy(x => x)));
        }
        else
        {
            for (var slot = 0; slot < NumberOfInventorySlots; slot++)
            {
                var equipped = GetItemInSlot((InventorySlot)slot, obj);
                if (GetIsObjectValid(equipped)) parts.Add($"slot {slot}: {DescribeLoadedObject(ctx, equipped)}");
            }
        }

        var locals = new List<string>();
        for (var index = 0; index < ObjectPlugin.GetLocalVariableCount(obj); index++)
        {
            var variable = ObjectPlugin.GetLocalVariable(obj, index);
            locals.Add(variable.Type switch
            {
                LocalVariableType.Int => $"{variable.Key}={GetLocalInt(obj, variable.Key)}",
                LocalVariableType.Float => $"{variable.Key}={GetLocalFloat(obj, variable.Key)}",
                LocalVariableType.String when variable.Key == "CONSTRUCTED_DROID" => $"{variable.Key}={DescribeDroid(ctx, GetLocalString(obj, variable.Key))}",
                LocalVariableType.String => $"{variable.Key}={GetLocalString(obj, variable.Key)}",
                _ => $"{variable.Key}:{variable.Type}",
            });
        }
        parts.Add("locals=" + string.Join(",", locals.OrderBy(x => x)));

        if (GetHasInventory(obj))
        {
            var contents = new List<string>();
            for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item); item = GetNextItemInInventory(obj))
                contents.Add(DescribeLoadedObject(ctx, item));
            parts.Add("contents=[" + string.Join(" / ", contents.OrderBy(x => x)) + "]");
        }
        return string.Join(" | ", parts);
    }

    private static string DescribeDroid(EngineTestContext ctx, string serialized)
    {
        var droid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
        string Part(string data) => string.IsNullOrWhiteSpace(data) ? "-" : DescribeStoredObject(ctx, data);
        return $"{{cpu={Part(droid.SerializedCPU)} head={Part(droid.SerializedHead)} body={Part(droid.SerializedBody)} " +
            $"arms={Part(droid.SerializedArms)} legs={Part(droid.SerializedLegs)} " +
            $"equipped=[{string.Join(" / ", droid.EquippedItems.OrderBy(x => x.Key).Select(x => $"{x.Key}:{Part(x.Value)}"))}] " +
            $"inventory=[{string.Join(" / ", droid.Inventory.OrderBy(x => x.Key).Select(x => $"{x.Key}:{Part(x.Value)}"))}]}}";
    }
}
