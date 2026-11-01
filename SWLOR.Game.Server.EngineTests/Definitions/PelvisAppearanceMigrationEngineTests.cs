using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Pelvis saved conversion matches native palettes and preserves edits through two reloads",
        Category = "SavedDataItemSweep PelvisAppearanceMigration", TimeoutSeconds = 120f)]
    public static async Task PelvisSavedColorsMatchNative(EngineTestContext ctx)
    {
        var actor = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        foreach (var scenario in new[] { "inherited", "explicit-zero", "sentinel", "per-part", "material-rgb", "global-rgb" })
        {
            var armor = await CreateItemAsync(ctx, actor, "gr_tunic", actor);
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ItemPlugin.SetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis, 35);
                ItemPlugin.SetItemAppearance(armor, ItemAppearanceType.ArmorColor, (int)AppearanceArmorColor.Cloth1, 71);
                ItemPlugin.SetItemAppearance(armor, ItemAppearanceType.ArmorColor, (int)AppearanceArmorColor.Cloth2, 93);
                var first = ArmorColorIndexCalculator.CalculatePerPart(AppearanceArmor.Pelvis, AppearanceArmorColor.Cloth1);
                var second = ArmorColorIndexCalculator.CalculatePerPart(AppearanceArmor.Pelvis, AppearanceArmorColor.Cloth2);
                ItemPlugin.SetItemAppearance(armor, ItemAppearanceType.ArmorColor, first, scenario == "sentinel" ? 255 : scenario == "per-part" ? 22 : 0);
                ItemPlugin.SetItemAppearance(armor, ItemAppearanceType.ArmorColor, second, scenario == "sentinel" ? 255 : scenario == "per-part" ? 44 : 0);
                if (scenario == "explicit-zero") SetLocalInt(armor, "APC_6_2", 1);
                var rgb = new TintMapColor(11, 133, 219).ToStoredValue();
                if (scenario == "material-rgb") SetLocalInt(armor, "TM_pfh0_pelvis035_4", rgb);
                if (scenario == "global-rgb") SetLocalInt(armor, "TMG_4", rgb);
                SetLocalInt(armor, "TM_pfh0_torso001_4", 103);
                SetLocalInt(armor, "PELVIS_TEST_STATE", 0);
                var identity = GetObjectUUID(armor);
                var legacy = ObjectPlugin.Serialize(armor);
                ctx.Assert(PelvisAppearanceMigration.MigrateObject(armor), scenario + ": native conversion changes pelvis35");
                var expected = PelvisNativeAppearance(armor);
                ctx.AssertEqual(71, GetItemAppearance(armor, ItemAppearanceType.ArmorColor, 2), "Global Cloth1 stays unchanged");
                ctx.AssertEqual(93, GetItemAppearance(armor, ItemAppearanceType.ArmorColor, 3), "Global Cloth2 stays unchanged");
                Invoke("MigrationObject", "DestroyTemporaryObject", armor);
                ctx.Assert(PelvisAppearanceMigration.MigrateSavedObject(legacy, out var saved), scenario + ": saved conversion changes pelvis35");
                var restored = Deserialize(ctx, saved);
                try
                {
                    ctx.AssertEqual(identity, GetObjectUUID(restored), scenario + ": identity survives migration");
                    ctx.AssertEqual(expected, PelvisNativeAppearance(restored), scenario + ": every native model/color and tint agrees");
                    ctx.AssertEqual(103, GetLocalInt(restored, "TM_pfh0_torso001_4"), "Sibling tint remains unchanged");
                    for (var cycle = 1; cycle <= 2; cycle++)
                    {
                        ctx.AssertEqual(cycle - 1, GetLocalInt(restored, "PELVIS_TEST_STATE"), "Gameplay state survived reload");
                        ItemPlugin.SetItemAppearance(restored, ItemAppearanceType.ArmorColor, first, 110 + cycle);
                        SetLocalInt(restored, "TM_pfh0_pelvis034_4", rgb + cycle);
                        SetLocalInt(restored, "PELVIS_TEST_STATE", cycle);
                        saved = ObjectPlugin.Serialize(restored);
                        Invoke("MigrationObject", "DestroyTemporaryObject", restored);
                        restored = OBJECT_INVALID;
                        ctx.Assert(!PelvisAppearanceMigration.MigrateSavedObject(saved, out _), "Saved retry leaves new player edits unchanged");
                        restored = Deserialize(ctx, saved);
                        ctx.Assert(!PelvisAppearanceMigration.MigrateObject(restored), "Native retry leaves new player edits unchanged");
                        ctx.AssertEqual(identity, GetObjectUUID(restored), "Identity survives repeated use and reload");
                        ctx.AssertEqual(1, GetItemStackSize(restored), "Armor quantity survives repeated reload");
                        ctx.AssertEqual(110 + cycle, GetItemAppearance(restored, ItemAppearanceType.ArmorColor, first), "Changed palette survives reload");
                        ctx.AssertEqual(rgb + cycle, GetLocalInt(restored, "TM_pfh0_pelvis034_4"), "Changed RGB survives reload");
                    }
                }
                finally
                {
                    if (GetIsObjectValid(restored)) Invoke("MigrationObject", "DestroyTemporaryObject", restored);
                }
            });
            await ctx.WaitFrameAsync();
        }
        ctx.SetResultDetail("Six legacy native palette/RGB cases matched saved conversion; global/sibling colors, UUIDs and quantities survived; migrated colors remained editable over two save/reload cycles.");
    }

    [EngineTest("Nested pelvis armor retains ownership and quantity through migration and repeated use",
        Category = "SavedDataItemSweep PelvisAppearanceMigration", TimeoutSeconds = 60f)]
    public static async Task PelvisNestedOwnershipSurvivesReload(EngineTestContext ctx)
    {
        var actor = ctx.SpawnCreature("civilian");
        var bag = await CreateItemAsync(ctx, actor, "bag_sm", actor);
        var armor = await CreateItemAsync(ctx, actor, "gr_tunic", bag);
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            ItemPlugin.SetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis, 35);
            ItemPlugin.SetItemAppearance(armor, ItemAppearanceType.ArmorColor, 2, 71);
            ItemPlugin.SetItemAppearance(armor, ItemAppearanceType.ArmorColor, 3, 93);
            var identity = GetObjectUUID(armor);
            var data = ObjectPlugin.Serialize(bag);
            Invoke("MigrationObject", "DestroyTemporaryObject", bag);
            ctx.Assert(PelvisAppearanceMigration.MigrateSavedObject(data, out data), "Nested legacy armor migrates");
            for (var cycle = 1; cycle <= 2; cycle++)
            {
                bag = Deserialize(ctx, data);
                try
                {
                    armor = GetFirstItemInInventory(bag);
                    ctx.Assert(GetIsObjectValid(armor), "The nested armor survives reload");
                    ctx.Assert(!GetIsObjectValid(GetNextItemInInventory(bag)), "Exactly one contained armor survives");
                    ctx.AssertEqual(bag, GetItemPossessor(armor, true), "The restored bag owns the same armor");
                    ctx.AssertEqual(identity, GetObjectUUID(armor), "Nested armor identity survives");
                    ctx.AssertEqual(1, GetItemStackSize(armor), "Nested armor quantity survives");
                    ctx.AssertEqual(34, GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis), "Nested armor uses pelvis34");
                    ctx.AssertEqual(cycle - 1, GetLocalInt(armor, "PELVIS_NESTED_STATE"), "Changed nested state survives reload");
                    SetLocalInt(armor, "PELVIS_NESTED_STATE", cycle);
                    data = ObjectPlugin.Serialize(bag);
                    ctx.Assert(!PelvisAppearanceMigration.MigrateSavedObject(data, out _), "Retry preserves edited nested state");
                }
                finally
                {
                    Invoke("MigrationObject", "DestroyTemporaryObject", bag);
                }
            }
        });
    }

    [EngineTest("Migrated pelvis RGB remains independent during real global dye changes and resets",
        Category = "SavedDataItemSweep PelvisAppearanceMigration", TimeoutSeconds = 120f)]
    public static async Task PelvisLegacyGlobalRgbRemainsEditable(EngineTestContext ctx)
    {
        var originalColor = new TintMapColor(43, 137, 211);
        var newGlobalColor = new TintMapColor(209, 51, 87);
        foreach (var savedConversion in new[] { false, true })
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var armor = await CreateItemAsync(ctx, owner, "gr_tunic", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetCreatureAppearanceType(owner, AppearanceType.Human);
                SetPhenoType(0, owner);
                ItemPlugin.SetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, 0);
                ItemPlugin.SetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso, 5);
                ItemPlugin.SetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis, 35);
                foreach (var layer in new[] { TintMapLayerType.Cloth1, TintMapLayerType.Cloth2 })
                {
                    SetLocalInt(armor, TintMapVariable.GetItemGlobalColorStateName(layer), originalColor.ToStoredValue());
                    DeleteLocalInt(armor, TintMapVariable.GetItemGlobalInheritanceStateName(layer));
                    foreach (var prefix in new[] { "pfh0", "pmh0" })
                        SetLocalInt(armor, TintMapVariable.GetName(prefix + "_chest005", layer), originalColor.ToStoredValue());
                }
                if (savedConversion)
                {
                    var legacy = ObjectPlugin.Serialize(armor);
                    ctx.Assert(PelvisAppearanceMigration.MigrateSavedObject(legacy, out var converted), "Saved RGB fixture migrates");
                    Invoke("MigrationObject", "DestroyTemporaryObject", armor);
                    var restored = Deserialize(ctx, converted);
                    armor = CopyItem(restored, owner, true);
                    ctx.Track(armor);
                    Invoke("MigrationObject", "DestroyTemporaryObject", restored);
                }
                else
                    ctx.Assert(PelvisAppearanceMigration.MigrateObject(armor), "Native RGB fixture migrates");
                SetItemCursedFlag(armor, false);
                SetPlotFlag(armor, false);
            });
            var oldArmor = GetItemInSlot(InventorySlot.Chest, owner);
            if (GetIsObjectValid(oldArmor))
                await ctx.ExecuteInCreatureContextAsync(owner, () => { SetItemCursedFlag(oldArmor, false); SetPlotFlag(oldArmor, false); });
            await EquipAsync(ctx, owner, armor, InventorySlot.Chest);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var selections = TintMapModelResolver.GetCurrentSelections(owner);
                var pelvis = selections.First(selection => selection.PaletteSource == armor && selection.ArmorPart == AppearanceArmor.Pelvis &&
                    selection.Material.Layers.Contains(TintMapLayerType.Cloth1) && selection.Material.Layers.Contains(TintMapLayerType.Cloth2));
                foreach (var layer in new[] { TintMapLayerType.Cloth1, TintMapLayerType.Cloth2 })
                {
                    var sibling = selections.First(selection => selection.PaletteSource == armor &&
                        selection.ArmorPart == AppearanceArmor.Torso && selection.Material.Layers.Contains(layer));
                    ctx.AssertEqual(originalColor, TintMapService.GetEffectiveDisplayColor(owner, pelvis, layer),
                        "Migration preserves the old pelvis RGB");
                    TintMapService.SetGlobalItemCustomColor(owner, selections, layer, newGlobalColor, armor);
                    ctx.AssertEqual(originalColor, TintMapService.GetEffectiveDisplayColor(owner, pelvis, layer),
                        "Changing the item-wide RGB preserves the migrated pelvis override");
                    ctx.AssertEqual(newGlobalColor, TintMapService.GetEffectiveDisplayColor(owner, sibling, layer),
                        "A sibling formerly inheriting global RGB follows the new global color");
                    TintMapService.ResetGlobalItemCustomColor(owner, selections, layer);
                    ctx.AssertEqual(originalColor, TintMapService.GetEffectiveDisplayColor(owner, pelvis, layer),
                        "Resetting the global RGB preserves the migrated pelvis override");
                    ctx.AssertEqual(0, GetLocalInt(armor, TintMapVariable.GetName(sibling.Material.Resref, layer)),
                        "Resetting the global RGB restores the sibling's native palette inheritance");
                    ctx.AssertEqual(0, GetLocalInt(armor, TintMapVariable.GetItemGlobalColorStateName(layer)),
                        "The real reset clears the global RGB state");
                }
            });
        }
        ctx.SetResultDetail("Native and saved migrations retain explicit pelvis RGB through real SetGlobalItemCustomColor/ResetGlobalItemCustomColor calls while inherited torso colors follow global edits and resets.");
    }

    private static string PelvisNativeAppearance(uint item)
    {
        var models = Enumerable.Range(0, (int)AppearanceArmor.Num)
            .Select(part => GetItemAppearance(item, ItemAppearanceType.ArmorModel, part));
        var colors = Enumerable.Range(0, 6 * (1 + (int)AppearanceArmor.Num))
            .Select(channel => GetItemAppearance(item, ItemAppearanceType.ArmorColor, channel));
        var locals = new List<string>();
        for (var index = 0; index < ObjectPlugin.GetLocalVariableCount(item); index++)
        {
            var variable = ObjectPlugin.GetLocalVariable(item, index);
            if (variable.Key.StartsWith("TM") || variable.Key.StartsWith("APC_"))
                locals.Add(variable.Key + "=" + GetLocalInt(item, variable.Key));
        }
        return string.Join(",", models) + ";" + string.Join(",", colors) + ";" + string.Join(",", locals.OrderBy(value => value));
    }
}
