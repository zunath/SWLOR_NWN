using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using AppearanceType = SWLOR.NWN.API.NWScript.Enum.AppearanceType;
using CreaturePart = SWLOR.NWN.API.NWScript.Enum.Creature.CreaturePart;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ItemAppearanceType = SWLOR.NWN.API.NWScript.Enum.Item.ItemAppearanceType;
using ItemPlugin = SWLOR.NWN.API.NWNX.ItemPlugin;
using ObjectPlugin = SWLOR.NWN.API.NWNX.ObjectPlugin;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class TintMapEngineTests
    {
        private readonly record struct NativeTintRow(string Material, string Parameter, int Type, float Value);

        [EngineTest("Tint Shuttle Pilot refresh installs authored helmet and chest dyes", Category = "Tint", TimeoutSeconds = 30f)]
        public static async Task ShuttlePilotRefreshInstallsAuthoredRows(EngineTestContext ctx)
        {
            var pilot = GetObjectByTag("novapilot");
            ctx.Assert(GetIsObjectValid(pilot), "The starter area's placed Shuttle Pilot must exist.");
            var helmet = GetItemInSlot(InventorySlot.Head, pilot);
            var armor = GetItemInSlot(InventorySlot.Chest, pilot);
            ctx.AssertEqual(114, GetItemAppearance(helmet, ItemAppearanceType.SimpleModel, 0), "Pilot helmet model");
            ctx.AssertEqual(249, GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso), "Pilot chest model");
            var originalHelmetColors = ReadArmorColors(helmet);
            var originalArmorColors = ReadArmorColors(armor);
            await RunAssignedAsync(ctx, GetArea(pilot), () => TintMapService.QueueRefresh(pilot));
            await ctx.WaitUntilAsync(() => ReadNativeRows(ctx, pilot).Any(row => row.Material == "helm_114"),
                5f, "the queued pilot tint refresh");
            var rows = ReadNativeRows(ctx, pilot);
            AssertNoResetRecords(ctx, rows);
            AssertNativeRow(ctx, rows, "helm_114", "rowcloth1", (704f + 135f + 0.5f) / 2048f);
            AssertNativeRow(ctx, rows, "helm_114", "rowleath1", (880f + 23f + 0.5f) / 2048f);
            AssertNativeRow(ctx, rows, "pfh0_chest249", "rowmetal1", (352f + 133f + 0.5f) / 2048f);
            AssertNativeRow(ctx, rows, "pfh0_chest249", "rowcloth1", (704f + 132f + 0.5f) / 2048f);
            ctx.Assert(originalHelmetColors.SequenceEqual(ReadArmorColors(helmet)), "Untinted helmet palette fields remain authored.");
            ctx.Assert(originalArmorColors.SequenceEqual(ReadArmorColors(armor)), "Untinted armor palette fields remain authored.");

            var selection = TintMapModelResolver.GetCurrentSelections(pilot).Single(s => s.Material.Resref == "helm_114");
            ctx.Assert(!RobeModelRenderer.SupportsRgb(selection), "The worn helmet cannot render exact RGB scalars.");
            var color = new TintMapColor(255, 0, 0);
            var layer = TintMapLayerType.Cloth1;
            var channel = (int)AppearanceArmorColor.Cloth1;
            await RunAssignedAsync(ctx, pilot, () =>
            {
                // Retain compatibility with RGB already persisted by older editor versions.
                TintMapService.SetColor(pilot, selection, layer, color);
                ctx.AssertEqual(TintMapPaletteColors.GetClosestColorId(layer, color),
                    GetItemAppearance(helmet, ItemAppearanceType.ArmorColor, channel), "Legacy RGB reaches the native helmet scheme as a preset.");
                ctx.AssertEqual(135, TintMapService.GetStandardColorId(pilot, selection, layer), "The authored helmet baseline survives projection.");
                TintMapService.ApplyCurrentColors(pilot);
                TintMapService.ResetColor(pilot, selection, layer);
                ctx.AssertEqual(135, GetItemAppearance(helmet, ItemAppearanceType.ArmorColor, channel), "Reset restores the authored helmet dye.");
                AssertProjectionCleared(ctx, helmet, channel);
            });
            ctx.Assert(originalArmorColors.SequenceEqual(ReadArmorColors(armor)), "Helmet projection never changes armor dyes.");
            ctx.SetResultDetail("Placed pilot retains authored helmet114 and chest249 dyes after a queued refresh. Server state only; client rendering is not attached.");
        }

        [EngineTest("Tint NPC spawn installs authored hair and clothing rows", Category = "Tint", TimeoutSeconds = 30f)]
        public static async Task CreatureSpawnInstallsAuthoredRows(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);

            // Do not invoke the tint service here: this verifies the actual spawn event and its
            // delayed refresh, independently of the explicit-refresh test below.
            await ctx.WaitUntilAsync(
                () => ReadNativeRows(ctx, civilian).Any(row => row.Parameter == "rowhair") &&
                      ReadNativeRows(ctx, civilian).Any(row => row.Material == "pfh0_robe187" && row.Parameter == "rowcloth1"),
                10f,
                "the NPC spawn hook to install native hair and dress palette rows");

            var rows = AssertAuthoredRows(ctx, civilian);
            ctx.SetResultDetail($"Automatic NPC spawn installed {rows.Count} native rows; hair=31, dress cloth1=174. Server state only; no client renderer is attached.");
        }

        [EngineTest("Tint NPC full refresh preserves all rows without native duplicates", Category = "Tint", TimeoutSeconds = 30f)]
        public static async Task CompleteRefreshReplacesStaleRowsWithoutDuplicates(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);

            // Seed both obsolete custom-color transport and incorrect current rows. A complete
            // refresh must remove them, then preserve every subsequently written palette row.
            List<NativeTintRow> firstRows = null;
            await RunAssignedAsync(ctx, civilian, () =>
            {
                SetMaterialShaderUniformVec4(civilian, string.Empty, "tintHair", 1f, 0f, 0f);
                SetMaterialShaderUniformInt(civilian, string.Empty, "useCustomHair", 1);
                SetMaterialShaderUniformVec4(civilian, string.Empty, "rowHair", 0f);
                SetMaterialShaderUniformVec4(civilian, "pfh0_robe187", "rowCloth1", 0f);
                var seededRows = ReadNativeRows(ctx, civilian);
                ctx.Assert(seededRows.Any(row => row.Parameter == "usecustomhair"),
                    "The native setter must install the obsolete parameter before testing its cleanup.");
                AssertNativeRow(ctx, seededRows, string.Empty, "rowhair", 0f);
                AssertNativeRow(ctx, seededRows, "pfh0_robe187", "rowcloth1", 0f);
                TintMapService.ApplyCurrentColors(civilian);
                firstRows = AssertAuthoredRows(ctx, civilian);
            });

            for (var repeat = 0; repeat < 5; repeat++)
            {
                await RunAssignedAsync(ctx, civilian, () =>
                {
                    TintMapService.ApplyCurrentColors(civilian);
                    var repeatedRows = AssertAuthoredRows(ctx, civilian);
                    ctx.AssertEqual(firstRows.Count, repeatedRows.Count, "Native parameter count after repeated refresh");
                });
            }

            await ctx.WaitFrameAsync();
            AssertAuthoredRows(ctx, civilian);
            ctx.SetResultDetail($"Six complete refreshes retained {firstRows.Count} native rows with no duplicate keys or obsolete parameters; hair=31, dress cloth1=174. Server state only; client replication/rendering remain outside this test.");
        }

        [EngineTest("Tint single-material edits preserve other rows without native reset records", Category = "Tint", TimeoutSeconds = 30f)]
        public static async Task MaterialColorEditsPreserveOtherRows(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            List<NativeTintRow> originalRows = null;
            await RunAssignedAsync(ctx, civilian, () =>
            {
                TintMapService.ApplyCurrentColors(civilian);
                originalRows = AssertAuthoredRows(ctx, civilian);
            });

            var dress = TintMapModelResolver.GetCurrentSelections(civilian)
                .Single(selection => selection.Material.Resref.Equals("pfh0_robe187", StringComparison.OrdinalIgnoreCase));
            var color = new TintMapColor(255, 0, 0);
            var colorId = TintMapPaletteColors.GetClosestColorId(TintMapLayerType.Cloth1, color);
            ctx.Assert(colorId != 174, "The edit must change the dress from its authored palette row.");
            var editedCoordinate = TintMapShaderColor.Encode(color);

            for (var repeat = 0; repeat < 3; repeat++)
            {
                await RunAssignedAsync(ctx, civilian, () =>
                {
                    TintMapService.SetColor(civilian, dress, TintMapLayerType.Cloth1, color);
                    var editedRows = ReadNativeRows(ctx, civilian);
                    AssertNoResetRecords(ctx, editedRows);
                    ctx.AssertEqual(originalRows.Count, editedRows.Count, "Native row count after individual material edit");
                    foreach (var original in originalRows)
                    {
                        var expected = original.Material == "pfh0_robe187" && original.Parameter == "rowcloth1"
                            ? editedCoordinate
                            : original.Value;
                        AssertNativeRow(ctx, editedRows, original.Material, original.Parameter, expected);
                    }
                });

                await RunAssignedAsync(ctx, civilian, () =>
                {
                    TintMapService.ResetColor(civilian, dress, TintMapLayerType.Cloth1);
                    AssertAuthoredRows(ctx, civilian);
                });
            }
            ctx.SetResultDetail($"Three dress-color edit/reset cycles preserved all {originalRows.Count} native rows with no type-zero reset records; unrelated skin, hair, tattoos and clothing stayed unchanged.");
        }

        [EngineTest("Tint Rodian bounty hunter feet and shins use authored leather dye", Category = "Tint", TimeoutSeconds = 30f)]
        public static async Task RodianEquipmentFallbacksInstallAuthoredLeatherRows(EngineTestContext ctx)
        {
            var hunter = ctx.SpawnCreature("malebh");
            await ctx.WaitUntilAsync(
                () => GetIsObjectValid(GetItemInSlot(InventorySlot.Chest, hunter)),
                10f,
                "the bounty hunter's authored outfit to be equipped");
            await ctx.WaitFrameAsync();

            await RunAssignedAsync(ctx, hunter, () =>
            {
                // The placed OOC hunter overrides the human blueprint's appearance. Match that
                // composed model while keeping its existing bountyhuntdred equipment and dyes.
                SetCreatureAppearanceType(hunter, (AppearanceType)10095);
                SetCreatureBodyPart(CreaturePart.Head, 56, hunter);
                SetColor(hunter, ColorChannel.Skin, 80);
                SetColor(hunter, ColorChannel.Hair, 20);
                SetColor(hunter, ColorChannel.Tattoo1, 53);
                SetColor(hunter, ColorChannel.Tattoo2, 68);
            });

            var expectedParts = new[]
            {
                (Part: AppearanceArmor.LeftFoot, Model: "pme0_footl247", Material: "pmh0_footl247", Number: 247),
                (Part: AppearanceArmor.RightFoot, Model: "pme0_footr247", Material: "pmh0_footr247", Number: 247),
                (Part: AppearanceArmor.LeftShin, Model: "pme0_shinl249", Material: "pmh0_shinl249", Number: 249),
                (Part: AppearanceArmor.RightShin, Model: "pme0_shinr249", Material: "pmh0_shinr249", Number: 249)
            };

            for (var repeat = 0; repeat < 2; repeat++)
            {
                await RunAssignedAsync(ctx, hunter, () =>
                {
                    ctx.AssertEqual("E", Get2DAString("appearance", "RACE", (int)GetAppearanceType(hunter)),
                        "Placed bounty hunter model race");
                    ctx.AssertEqual(0, (int)GetGender(hunter), "Placed bounty hunter model gender");
                    ctx.AssertEqual(0, (int)GetPhenoType(hunter), "Placed bounty hunter model phenotype");
                    var outfit = GetItemInSlot(InventorySlot.Chest, hunter);
                    ctx.AssertEqual("bountyhuntdred", GetResRef(outfit), "Bounty hunter outfit blueprint");
                    ctx.AssertEqual(23,
                        GetItemAppearance(outfit, ItemAppearanceType.ArmorColor, (int)AppearanceArmorColor.Leather2),
                        "Authored bounty hunter leather2 palette index");
                    var selections = TintMapModelResolver.GetCurrentSelections(hunter);
                    foreach (var expected in expectedParts)
                    {
                        ctx.AssertEqual(expected.Number,
                            GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)expected.Part),
                            $"Authored {expected.Part} model");
                        var matches = selections.Where(selection =>
                                selection.ModelResref.Equals(expected.Model, StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                        ctx.AssertEqual(1, matches.Length, $"Resolved tint selection for {expected.Model}");
                        var selection = matches[0];
                        ctx.AssertEqual(expected.Material, selection.Material.Resref.ToLowerInvariant(),
                            $"Canonical tint material for {expected.Model}");
                        ctx.Assert(selection.Material.Layers.SequenceEqual(new[] { TintMapLayerType.Leather2 }),
                            $"{expected.Model} must expose its authored leather2 layer.");
                        ctx.AssertEqual(outfit, selection.GetPaletteSource(TintMapLayerType.Leather2),
                            $"Equipment dye source for {expected.Model}");
                        ctx.Assert(selection.UsesItemColor(TintMapLayerType.Leather2),
                            $"{expected.Model} must read armor dyes.");
                        ctx.AssertEqual(23,
                            TintMapService.GetStandardColorId(hunter, selection, TintMapLayerType.Leather2),
                            $"Resolved authored leather2 palette index for {expected.Model}");
                    }

                    TintMapService.ApplyCurrentColors(hunter);
                    var rows = ReadNativeRows(ctx, hunter);
                    AssertNoResetRecords(ctx, rows);
                    foreach (var expected in expectedParts)
                        AssertNativeRow(ctx, rows, expected.Material, "rowleath2", (880f + 23f + 0.5f) / 2048f);
                    AssertNativeRow(ctx, rows, string.Empty, "rowskin", (80f + 0.5f) / 2048f);
                    AssertNativeRow(ctx, rows, string.Empty, "rowhair", (176f + 20f + 0.5f) / 2048f);
                });
            }

            ctx.SetResultDetail("Race E male phenotype 0 resolved both feet 247 and shins 249 to canonical human materials; two refreshes installed all four leather2=23 rows without reset records. Server state only; no client renderer is attached.");
        }

        [EngineTest("Tint native robe palette preserves authored colors and restores custom edits", Category = "Tint", TimeoutSeconds = 30f)]
        public static async Task NativeRobePalettePreservesAuthoredColorsAndResets(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                // Large-body robes retain the native compatibility path.
                SetPhenoType((SWLOR.NWN.API.NWScript.Enum.PhenoType)2, civilian);
                var outfit = GetItemInSlot(InventorySlot.Chest, civilian);
                var originalItemColors = ReadArmorColors(outfit);
                var originalCreatureColors = new[] { ColorChannel.Skin, ColorChannel.Hair, ColorChannel.Tattoo1, ColorChannel.Tattoo2 }
                    .Select(channel => GetColor(civilian, channel)).ToArray();
                TintMapService.ApplyCurrentColors(civilian);
                ctx.Assert(originalItemColors.SequenceEqual(ReadArmorColors(outfit)),
                    "An authored NPC with no custom tint must retain every raw armor palette field.");
                ctx.Assert(originalCreatureColors.SequenceEqual(
                        new[] { ColorChannel.Skin, ColorChannel.Hair, ColorChannel.Tattoo1, ColorChannel.Tattoo2 }
                            .Select(channel => GetColor(civilian, channel))),
                    "An authored NPC with no custom tint must retain every native creature palette field.");
                for (var channel = 0; channel < 120; channel++)
                {
                    AssertProjectionCleared(ctx, outfit, channel);
                    AssertProjectionCleared(ctx, civilian, channel);
                }

                var dress = GetCivilianDress(civilian);
                var color = new TintMapColor(255, 0, 0);
                var nativeColor = TintMapPaletteColors.GetClosestColorId(TintMapLayerType.Cloth1, color);
                var index = ArmorColorIndexCalculator.CalculatePerPart(AppearanceArmor.Robe, AppearanceArmorColor.Cloth1);
                ctx.Assert(nativeColor != 174, "The custom color must differ from the authored brown dress.");
                TintMapService.SetColor(civilian, dress, TintMapLayerType.Cloth1, color);
                ctx.AssertEqual(nativeColor, GetItemAppearance(outfit, ItemAppearanceType.ArmorColor, index),
                    "Custom robe cloth1 must reach the native PLT palette field");
                ctx.AssertEqual(174, TintMapService.GetStandardColorId(civilian, dress, TintMapLayerType.Cloth1),
                    "The preset getter must retain the authored dress color beneath the projection");
                var restored = ObjectPlugin.Deserialize(ObjectPlugin.Serialize(outfit));
                ctx.Assert(GetIsObjectValid(restored), "The projected item snapshot must deserialize.");
                ctx.Track(restored);
                var restoredDress = new TintMapMaterialSelection(dress.ModelResref, dress.Material,
                    restored, civilian, true, AppearanceArmor.Robe);
                ctx.AssertEqual(nativeColor, GetItemAppearance(restored, ItemAppearanceType.ArmorColor, index),
                    "Droid-style item serialization must preserve the native projection");
                ctx.AssertEqual(174, TintMapService.GetStandardColorId(civilian, restoredDress, TintMapLayerType.Cloth1),
                    "Droid-style item serialization must preserve the original preset baseline");
                DeleteLocalInt(restored, TintMapNativePaletteProjection.BaselineName(index));
                DeleteLocalInt(restored, TintMapNativePaletteProjection.LastAppliedName(index));
                TintMapService.ReplaceItemTintOverrides(outfit, restored);
                ctx.AssertEqual(174, TintMapService.GetStandardColorId(civilian, restoredDress, TintMapLayerType.Cloth1),
                    "Equipment replacement must carry the native projection baseline with its tint locals");
                var projectedItemColors = ReadArmorColors(outfit);
                for (var channel = 0; channel < projectedItemColors.Length; channel++)
                    if (channel != index)
                        ctx.AssertEqual(originalItemColors[channel], projectedItemColors[channel],
                            $"A robe edit must not alter unrelated armor palette field {channel}");

                TintMapService.ResetColor(civilian, dress, TintMapLayerType.Cloth1);
                ctx.AssertEqual(174, TintMapService.GetStandardColorId(civilian, dress, TintMapLayerType.Cloth1),
                    "Reset restores the authored brown dress");
                ctx.AssertEqual(255, GetItemAppearance(outfit, ItemAppearanceType.ArmorColor, index),
                    "Reset restores native global-color inheritance for the authored robe");
                AssertProjectionCleared(ctx, outfit, index);

                // Robe187 exposes skin, so a custom semantic skin color must also reach the
                // native creature field. Hair is on the head only and requires no native write.
                var skinColor = TintMapPaletteColors.GetClosestColorId(TintMapLayerType.Skin, color);
                ctx.Assert(skinColor != 2, "The custom skin color must differ from the authored palette.");
                TintMapService.SetCreatureCustomColor(civilian, TintMapModelResolver.GetCurrentSelections(civilian),
                    TintMapLayerType.Skin, color);
                ctx.AssertEqual(skinColor, GetColor(civilian, ColorChannel.Skin), "Native robe skin projection");
                ctx.AssertEqual(2, TintMapService.GetStandardColorId(civilian, dress, TintMapLayerType.Skin),
                    "Native skin projection must preserve the original preset");
                TintMapService.SetCreatureCustomColor(civilian, TintMapModelResolver.GetCurrentSelections(civilian),
                    TintMapLayerType.Hair, color);
                ctx.AssertEqual(31, GetColor(civilian, ColorChannel.Hair),
                    "A head-only hair tint must not rewrite the native creature palette");
                TintMapService.ResetCreatureCustomColor(civilian, TintMapLayerType.Skin);
                TintMapService.ResetCreatureCustomColor(civilian, TintMapLayerType.Hair);
                ctx.AssertEqual(2, GetColor(civilian, ColorChannel.Skin), "Skin reset restores authored native color");
                AssertProjectionCleared(ctx, civilian, (int)TintMapLayerType.Skin);
                // Restore the normal head registration before checking all authored rows.
                SetPhenoType(SWLOR.NWN.API.NWScript.Enum.PhenoType.Normal, civilian);
                TintMapService.ApplyCurrentColors(civilian);
                AssertAuthoredRows(ctx, civilian);
            });
            await ctx.DelaySecondsAsync(0.5f);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                AssertAuthoredRows(ctx, civilian);
                ctx.AssertEqual(2, GetColor(civilian, ColorChannel.Skin),
                    "Queued refreshes must read the reset state instead of restoring an earlier custom projection");
                AssertProjectionCleared(ctx, civilian, (int)TintMapLayerType.Skin);
            });
            ctx.SetResultDetail("Native armor fields stayed unchanged without tint; custom robe cloth1 and skin reached native PLT fields, retained authored presets, and reset to brown/skin2. Head-only hair avoided a native write. No client renderer is attached.");
        }

        [EngineTest("Tint native robe palette retains external presets and per-part inheritance", Category = "Tint", TimeoutSeconds = 30f)]
        public static async Task NativeRobePaletteRetainsExternalPresetsAndInheritance(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                SetPhenoType((SWLOR.NWN.API.NWScript.Enum.PhenoType)2, civilian);
                var outfit = GetItemInSlot(InventorySlot.Chest, civilian);
                var dress = GetCivilianDress(civilian);
                var layer = TintMapLayerType.Cloth1;
                var channel = AppearanceArmorColor.Cloth1;
                var index = ArmorColorIndexCalculator.CalculatePerPart(AppearanceArmor.Robe, channel);
                var marker = ArmorColorIndexCalculator.GetPerPartOverrideVariableName(AppearanceArmor.Robe, channel);
                var color = Enumerable.Range(1, TintMapMaterialRegistry.PaletteColorCount - 1)
                    .Select(colorId => TintMapPaletteColors.GetColor(layer, colorId))
                    .First(candidate => TintMapPaletteColors.GetClosestColorId(layer, candidate) is not (0 or 56 or 77 or 89));
                var projected = TintMapPaletteColors.GetClosestColorId(layer, color);
                ctx.Assert(projected != 77 && projected != 89, "The custom row must differ from both external presets.");

                SetLocalInt(outfit, marker, 1);
                ItemPlugin.SetItemAppearance(outfit, ItemAppearanceType.ArmorColor, index, 77, false);
                TintMapService.SetColor(civilian, dress, layer, color);
                ctx.AssertEqual(77, TintMapService.GetStandardColorId(civilian, dress, layer), "Explicit per-part baseline");
                // A native preset writer can change the armor while the persisted custom tint
                // remains active. The next refresh must remember that new authored baseline.
                ItemPlugin.SetItemAppearance(outfit, ItemAppearanceType.ArmorColor, index, 89, false);
                ctx.AssertEqual(89, TintMapService.GetStandardColorId(civilian, dress, layer), "External preset before refresh");
                TintMapService.ApplyCurrentColors(civilian);
                ctx.AssertEqual(projected, GetItemAppearance(outfit, ItemAppearanceType.ArmorColor, index),
                    "The active custom tint remains projected after an external preset edit");
                TintMapService.ResetColor(civilian, dress, layer);
                ctx.AssertEqual(89, GetItemAppearance(outfit, ItemAppearanceType.ArmorColor, index),
                    "Reset preserves the newer external per-part preset");
                AssertProjectionCleared(ctx, outfit, index);

                foreach (var rawInherited in new[] { 255, 0 })
                {
                    DeleteLocalInt(outfit, marker);
                    ItemPlugin.SetItemAppearance(outfit, ItemAppearanceType.ArmorColor, index, rawInherited, false);
                    TintMapService.SetColor(civilian, dress, layer, color);
                    ItemPlugin.SetItemAppearance(outfit, ItemAppearanceType.ArmorColor, (int)channel, 56, false);
                    TintMapService.ApplyCurrentColors(civilian);
                    ctx.AssertEqual(56, TintMapService.GetStandardColorId(civilian, dress, layer),
                        $"Raw {rawInherited} inheritance must read a later global preset edit");
                    TintMapService.ResetColor(civilian, dress, layer);
                    ctx.AssertEqual(255, GetItemAppearance(outfit, ItemAppearanceType.ArmorColor, index),
                        $"Reset must restore native inheritance from raw {rawInherited}");
                    ctx.AssertEqual(56, GetItemAppearance(outfit, ItemAppearanceType.ArmorColor, (int)channel),
                        "Reset must not replace an externally edited global dye");
                    AssertProjectionCleared(ctx, outfit, index);
                }

                SetLocalInt(outfit, marker, 1);
                ItemPlugin.SetItemAppearance(outfit, ItemAppearanceType.ArmorColor, index, 0, false);
                TintMapService.SetColor(civilian, dress, layer, color);
                TintMapService.ResetColor(civilian, dress, layer);
                ctx.AssertEqual(0, GetItemAppearance(outfit, ItemAppearanceType.ArmorColor, index),
                    "An explicitly selected per-part palette zero must remain zero");
                ctx.AssertEqual(0, TintMapService.GetStandardColorId(civilian, dress, layer),
                    "The explicit zero marker must survive projection and reset");
                AssertProjectionCleared(ctx, outfit, index);
            });
            ctx.SetResultDetail("Native field tests preserved explicit per-part edits 77→89, raw255 and legacy raw0 global inheritance, later global dye56, and explicitly selected palette0 through custom edit/reset. No client renderer is attached.");
        }

        private static TintMapMaterialSelection GetCivilianDress(uint civilian) =>
            TintMapModelResolver.GetCurrentSelections(civilian)
                .Single(selection => selection.Material.Resref.Equals("pfh0_robe187", StringComparison.OrdinalIgnoreCase));

        private static int[] ReadArmorColors(uint item) => Enumerable.Range(0, 120)
            .Select(channel => GetItemAppearance(item, ItemAppearanceType.ArmorColor, channel)).ToArray();

        private static void AssertProjectionCleared(EngineTestContext ctx, uint target, int channel)
        {
            ctx.AssertEqual(0, GetLocalInt(target, TintMapNativePaletteProjection.BaselineName(channel)),
                $"Projection baseline {channel} must be absent");
            ctx.AssertEqual(0, GetLocalInt(target, TintMapNativePaletteProjection.LastAppliedName(channel)),
                $"Projection last-applied {channel} must be absent");
        }

        private static async Task RunAssignedAsync(EngineTestContext ctx, uint creature, Action action)
        {
            // NWNX schedules AssignCommand on the AI event queue; it does not invoke the closure
            // inline. Read/assert inside that closure and wait for its completion, otherwise an
            // empty table observed before the assignment runs looks like a failed native setter.
            var completed = false;
            Exception failure = null;
            AssignCommand(creature, () =>
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    // ClosureManager logs and swallows uncaught closure exceptions. Bring them
                    // back to the test task so a native assertion failure cannot silently pass.
                    failure = exception;
                }
                finally
                {
                    completed = true;
                }
            });
            await ctx.WaitUntilAsync(() => completed, 5f, "the assigned native tint refresh to finish");
            if (failure != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }

        private static async Task<uint> SpawnCivilianAsync(EngineTestContext ctx)
        {
            // This is the same authored female/head121/robe187 combination as the OOC NPC.
            // Its blueprint already equips farmer_outfit002, preserving actual authored colors.
            var civilian = ctx.SpawnCreature("civilian");
            await ctx.WaitUntilAsync(
                () => GetIsObjectValid(GetItemInSlot(InventorySlot.Chest, civilian)),
                10f,
                "the civilian's authored outfit to be equipped");
            await ctx.WaitFrameAsync();

            var outfit = GetItemInSlot(InventorySlot.Chest, civilian);
            ctx.AssertEqual("farmer_outfit002", GetResRef(outfit), "Civilian outfit blueprint");
            ctx.AssertEqual(31, GetColor(civilian, ColorChannel.Hair), "Authored civilian hair palette index");
            ctx.AssertEqual(2, GetColor(civilian, ColorChannel.Skin), "Authored civilian skin palette index");
            ctx.AssertEqual(174,
                GetItemAppearance(outfit, ItemAppearanceType.ArmorColor, (int)AppearanceArmorColor.Cloth1),
                "Authored outfit cloth1 palette index");
            return civilian;
        }

        private static List<NativeTintRow> AssertAuthoredRows(EngineTestContext ctx, uint civilian)
        {
            var rows = ReadNativeRows(ctx, civilian);
            AssertNoResetRecords(ctx, rows);
            var duplicates = rows.GroupBy(row => (row.Material, row.Parameter))
                .Where(group => group.Count() != 1)
                .Select(group => $"{group.Key.Material}/{group.Key.Parameter} ({group.Count()})")
                .ToArray();
            ctx.Assert(duplicates.Length == 0, $"Native material parameters must have unique keys: {string.Join(", ", duplicates)}.");

            var expected = new Dictionary<(string Material, string Parameter), float>();
            var selections = TintMapModelResolver.GetCurrentSelections(civilian);
            foreach (var selection in selections)
            {
                foreach (var layer in selection.Material.Layers)
                {
                    var definition = TintMapMaterialRegistry.GetLayer(layer);
                    var material = selection.Material.Resref.ToLowerInvariant();
                    var paletteId = TintMapService.GetStandardColorId(civilian, selection, layer);
                    var uniform = definition.UniformName.ToLowerInvariant();
                    var coordinate = (definition.PaletteBaseRow + paletteId + 0.5f) / 2048f;
                    expected[(material, uniform)] = coordinate;
                    // Semantic colors also have named attachment records so replacements
                    // receive the same dye after the client's wildcard update.
                    if (TintMapVariable.IsCreatureColorLayer(layer))
                        expected[(string.Empty, uniform)] = coordinate;
                }
            }

            ctx.Assert(expected.Count > 4, "The live tint registry must resolve clothing as well as creature color layers.");
            ctx.AssertEqual(expected.Count, rows.Count, "Complete native material parameter count (including obsolete parameters)");
            foreach (var (key, coordinate) in expected)
                AssertNativeRow(ctx, rows, key.Material, key.Parameter, coordinate);

            // Independent values for the reported regression: using only production palette
            // resolution as the oracle could allow the same wrong default to pass both sides.
            AssertNativeRow(ctx, rows, string.Empty, "rowhair", (176f + 31f + 0.5f) / 2048f);
            AssertNativeRow(ctx, rows, string.Empty, "rowskin", (2f + 0.5f) / 2048f);
            AssertNativeRow(ctx, rows, "pfh0_robe187", "rowcloth1", (704f + 174f + 0.5f) / 2048f);
            AssertNativeRow(ctx, rows, "pfh0_robe187", "rowcloth2", (704f + 3f + 0.5f) / 2048f);
            return rows;
        }

        private static void AssertNoResetRecords(EngineTestContext ctx, IReadOnlyList<NativeTintRow> rows)
        {
            var resetRecords = rows.Where(row => row.Type == 0)
                .Select(row => $"{row.Material}/{row.Parameter}")
                .ToArray();
            ctx.Assert(resetRecords.Length == 0,
                $"Native type-zero records reset every client material parameter and must not remain among tint rows: {string.Join(", ", resetRecords)}.");
        }

        private static void AssertNativeRow(
            EngineTestContext ctx,
            IReadOnlyList<NativeTintRow> rows,
            string material,
            string parameter,
            float expected)
        {
            var matches = rows.Where(row => row.Material == material && row.Parameter == parameter).ToArray();
            ctx.AssertEqual(1, matches.Length, $"Native row {material}/{parameter} entry count");
            ctx.AssertEqual(2, matches[0].Type, $"Native row {material}/{parameter} float transport type");
            ctx.Assert(Math.Abs(expected - matches[0].Value) < 0.0000001f,
                $"Native row {material}/{parameter}: expected {expected:R}, got {matches[0].Value:R}.");
        }

        internal static void AssertNativeRgb(EngineTestContext ctx, uint creature, string material, TintMapLayerType layer, TintMapColor color)
        {
            var rows = ReadNativeRows(ctx, creature);
            AssertNoResetRecords(ctx, rows);
            var uniform = TintMapMaterialRegistry.GetLayer(layer).UniformName.ToLowerInvariant();
            var matches = rows.Where(row => row.Material == material.ToLowerInvariant() && row.Parameter == uniform).ToArray();
            ctx.AssertEqual(1, matches.Length, "One atomic RGB scalar for " + material + "/" + uniform);
            ctx.AssertEqual(TintMapShaderColor.Encode(color), matches[0].Value, "All RGB bits survive native material storage");
        }

        private static List<NativeTintRow> ReadNativeRows(EngineTestContext ctx, uint civilian)
        {
            var nativeCreature = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(civilian);
            ctx.Assert(nativeCreature != null, "The spawned NPC must have a native creature object.");
            var parameters = nativeCreature.m_lMaterialShaderParameters;
            var rows = new List<NativeTintRow>(parameters.Count);
            for (var index = 0; index < parameters.Count; index++)
            {
                var parameter = parameters[index];
                rows.Add(new NativeTintRow(
                    ReadNativeName(parameter.m_sMaterialName),
                    ReadNativeName(parameter.m_sParamName),
                    parameter.m_nType,
                    parameter.m_fValue1));
            }
            return rows;
        }

        private static string ReadNativeName(NativeArray<byte> bytes)
        {
            var result = new StringBuilder();
            for (var index = 0; index < bytes.Length && bytes[index] != 0; index++)
                result.Append((char)bytes[index]);
            return result.ToString().ToLowerInvariant();
        }
    }
}
