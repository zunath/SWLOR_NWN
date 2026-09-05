using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using AppearanceType = SWLOR.NWN.API.NWScript.Enum.AppearanceType;
using CreaturePart = SWLOR.NWN.API.NWScript.Enum.Creature.CreaturePart;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ItemAppearanceType = SWLOR.NWN.API.NWScript.Enum.Item.ItemAppearanceType;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class TintMapEngineTests
    {
        private readonly record struct NativeTintRow(string Material, string Parameter, int Type, float Value);

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
            var editedCoordinate = (704f + colorId + 0.5f) / 2048f;

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
                    var material = TintMapVariable.IsCreatureColorLayer(layer) ? string.Empty : selection.Material.Resref.ToLowerInvariant();
                    var paletteId = TintMapService.GetStandardColorId(civilian, selection, layer);
                    expected[(material, definition.UniformName.ToLowerInvariant())] =
                        (definition.PaletteBaseRow + paletteId + 0.5f) / 2048f;
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
