using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    private const string SavedDataSweepCategory = "SavedDataItemSweep";
    private const string CurrentLootLabel = "already migrated loot";
    private static readonly Assembly SavedDataSweepAssembly = typeof(EquipmentTypeMigration).Assembly;

    /// <summary>
    /// The saved-data sweep must save what the engine sweep saves. Both converters run on the same legacy payloads,
    /// both results load into the engine, and everything the engine reads back is compared. The production-copy
    /// parity check holds few of these legacy kinds, so this covers each rule the saved-data sweep mirrors.
    /// </summary>
    [EngineTest("Saved-data stored-item sweep saves what the engine sweep saves for every legacy item kind", Category = SavedDataSweepCategory, TimeoutSeconds = 400f)]
    public static async Task SavedDataItemSweepMatchesEngine(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        var savedCreature = ctx.SpawnCreature("civilian", 2f);
        await ctx.WaitFrameAsync();
        // Fixtures whose saved root converts, so both sweeps must report its refreshed listing.
        var rootConversions = new HashSet<string> { "legacy Ruptor visor", "legacy Thermal visor" };
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
                rootConversions.Add($"legacy {resref}");
                DestroyObject(item);
            });
        }

        foreach (var resref in ItemAppearanceCatalogMigration.Revisions.Keys)
        {
            var item = await CreateItemAsync(ctx, owner, resref, owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                StampLegacyAppearance(ctx, item);
                fixtures.Add(($"legacy appearance {resref}", ObjectPlugin.Serialize(item), true));
                rootConversions.Add($"legacy appearance {resref}");
                DestroyObject(item);
            });
        }

        // A catalog pistol saved with arrow ammunition from its legacy bow base item: the engine re-saves it
        // with the ammunition type it derives from the base item, the saved-data sweep keeps the saved value.
        var arrowPistol = await CreateItemAsync(ctx, owner, "raider_pistol", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            StampLegacyAppearance(ctx, arrowPistol);
            ItemPlugin.SetBaseItemType(arrowPistol, BaseItem.Pistol);
            RemoveMatching(arrowPistol, ItemPropertyType.UnlimitedAmmunition, 0);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.UnlimitedAmmunition, 0, 1), arrowPistol);
            ItemPlugin.SetBaseItemType(arrowPistol, BaseItem.Sling);
            fixtures.Add(("legacy appearance pistol with arrow ammunition", ObjectPlugin.Serialize(arrowPistol), true));
            rootConversions.Add("legacy appearance pistol with arrow ammunition");
            DestroyObject(arrowPistol);
        });

        // Shipped breastplates: retired Ice Defense beside an Ice Resistance enhancement, the same copy after
        // losing the retired property, Ice Defense the combat replacement already converted, and no Ice at all.
        var arkanianEnhancements = new Dictionary<(ItemPropertyType, int), int>
        {
            [(ItemPropertyType.HPBonus, 0)] = 40,
            [(ItemPropertyType.Defense, (int)CombatDamageType.Physical)] = 3,
        };
        foreach (var (iceDefense, iceResistance) in new[] { (30, 5), (0, 5), (0, 30), (0, 0) })
        {
            var armor = await CreateItemAsync(ctx, owner, "ark_armor", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                StampLegacyArkanianArmor(armor, arkanianEnhancements, iceDefense, iceResistance);
                fixtures.Add(($"legacy Arkanian breastplate (Ice Defense {iceDefense}, Ice Resistance {iceResistance})",
                    ObjectPlugin.Serialize(armor), true));
                DestroyObject(armor);
            });
        }

        // Every shipped item with a retired elemental Defense: the Defense itself, and the Defense the combat
        // replacement already converted into Resistance, here above the retired amount (capped at 100).
        foreach (var resref in RetiredDefenseStatRevisionCatalog.Changes.Keys)
        foreach (var converted in new[] { false, true })
        {
            var retired = RetiredDefenseStatRevisionCatalog.RetiredDefense(resref);
            var item = await CreateItemAsync(ctx, owner, resref, owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                StampLegacyRetiredDefense(item, (CombatDamageType)retired.Subtype, retired.Previous, converted);
                fixtures.Add(($"legacy {resref} ({(converted ? "converted" : "retired")} {(CombatDamageType)retired.Subtype} {retired.Previous})",
                    ObjectPlugin.Serialize(item), true));
                DestroyObject(item);
            });
        }

        // Droid parts and controllers saved before the Unarmed and Twin Pistol skills: a CPU, and a controller with
        // a boosted Katar and zero ranged skills that saves that CPU.
        var legacyCpu = await CreateItemAsync(ctx, owner, LegacyDroidCpu, owner);
        var legacyController = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            StampLegacyDroidCpu(legacyCpu);
            StampLegacyDroidController(legacyController, ObjectPlugin.Serialize(legacyCpu));
            fixtures.Add(("legacy droid CPU without Unarmed and Twin Pistol", ObjectPlugin.Serialize(legacyCpu), true));
            fixtures.Add(("legacy droid controller without Unarmed and Twin Pistol", ObjectPlugin.Serialize(legacyController), true));
            DestroyObject(legacyCpu);
            DestroyObject(legacyController);
        });

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
            // creature items after loading them, so loading the saved-data result must yield the same pistol.
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
            fixtures.Add(("legacy loot with a temporary Attack bonus", ObjectPlugin.Serialize(buffedGloves), true));
            fixtures.Add(("legacy artwork", ObjectPlugin.Serialize(artwork), true));
            fixtures.Add((CurrentLootLabel, ObjectPlugin.Serialize(current), true));
            fixtures.Add(("container with legacy contents", ObjectPlugin.Serialize(bag), true));
            fixtures.Add(("droid controller with legacy parts", ObjectPlugin.Serialize(controller), true));
            fixtures.Add(("saved creature carrying legacy items", ObjectPlugin.Serialize(savedCreature), true));
        });

        // Every fixture is checked before the test reports, so one run lists every difference.
        var failures = new List<string>();
        foreach (var (label, data, repairHeadgear) in fixtures)
        {
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                void Check(bool condition, string message)
                {
                    if (!condition) failures.Add($"{label}: {message}");
                }
                try
                {
                    var engine = ConvertStoredItem("EngineStoredItemConverter", data, repairHeadgear);
                    var savedData = ConvertStoredItem("SavedDataStoredItemConverter", data, repairHeadgear);
                    Check(engine.Changed == (label != CurrentLootLabel), $"the engine sweep changed it: {engine.Changed}");
                    Check(engine.Changed == savedData.Changed, $"engine changed {engine.Changed}, saved-data changed {savedData.Changed}");
                    Check(engine.Listing == savedData.Listing, $"engine listing '{engine.Listing}', saved-data listing '{savedData.Listing}'");
                    Check(rootConversions.Contains(label) == (engine.Listing.Length > 0), $"reported listing '{engine.Listing}'");
                    var engineObject = DescribeStoredObject(ctx, engine.Migrated);
                    var savedObject = DescribeStoredObject(ctx, savedData.Migrated);
                    Check(engineObject == savedObject, $"the engine reads different objects:\n  engine:  {engineObject}\n  saved:   {savedObject}");
                    Check(!ConvertStoredItem("EngineStoredItemConverter", savedData.Migrated, repairHeadgear).Changed,
                        "the engine sweep finds more to change in the saved-data result");
                    Check(!ConvertStoredItem("SavedDataStoredItemConverter", savedData.Migrated, repairHeadgear).Changed,
                        "the saved-data sweep is not idempotent");
                }
                catch (Exception ex)
                {
                    failures.Add($"{label}: {ex.GetBaseException().Message}");
                }
            });
            await ctx.WaitFrameAsync();
        }
        ctx.Assert(failures.Count == 0, $"{failures.Count} of {fixtures.Count} fixtures differ:\n" + string.Join("\n", failures));
    }

    private static void StampLegacyRetiredDefense(uint item, CombatDamageType damageType, int amount, bool converted)
    {
        RemoveMatching(item, ItemPropertyType.Defense, (int)CombatDamageType.Physical);
        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Physical, 23), item);
        if (converted)
        {
            damageType.TryGetElementalResistanceType(out var resistance);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Resistance, (int)resistance,
                Resistance.EncodeItemPropertyCostTableValue(Math.Min(amount + 5, Resistance.MaximumResistance))), item);
        }
        else
            // Current constructors reject the retired elemental Defense subtypes, so they are written natively.
            AddLegacyProperty(item, ItemPropertyType.Defense, (int)damageType, 35, amount);
        DeleteLocalInt(item, RetiredDefenseStatRevisionCatalog.RevisionVariable);
    }

    private static (bool Changed, string Migrated, string Listing) ConvertStoredItem(string converterName, string data, bool repairHeadgear)
    {
        var converter = SavedDataSweepAssembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition." + converterName);
        var listingType = SavedDataSweepAssembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.RootListing");
        var listings = new List<object>();
        var capture = typeof(MigrationEngineTests).GetMethod(nameof(CaptureRootListing), BindingFlags.NonPublic | BindingFlags.Static)
            .MakeGenericMethod(listingType).Invoke(null, new object[] { listings });
        var arguments = new[] { data, null, repairHeadgear, capture };
        var changed = (bool)converter.GetMethod("MigrateStoredObjectData").Invoke(converter.GetField("Instance").GetValue(null), arguments);
        return (changed, (string)arguments[1], string.Join("; ", listings));
    }

    private static Action<T> CaptureRootListing<T>(List<object> sink) => value => sink.Add(value);

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
