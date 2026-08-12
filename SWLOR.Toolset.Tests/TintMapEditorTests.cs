using System.Text;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Creatures;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Creatures;
using SWLOR.Toolset.Editors.Items;
using SWLOR.Toolset.Editors.TintMaps;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public class TintMapEditorTests
    {
        private static string RepoRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    if (File.Exists(Path.Combine(current.FullName, "Build", "hakbuilder.json")) &&
                        Directory.Exists(Path.Combine(current.FullName, "SWLOR_Haks")))
                    {
                        return current.FullName;
                    }

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException("Could not locate the repository root.");
            }
        }

        private static ResourceIndex Resources() =>
            ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks"));

        private static RenderModel ModelWith(string material) =>
            new()
            {
                Meshes = new[]
                {
                    new RenderMesh
                    {
                        NodeName = "sample",
                        TextureName = material,
                        MaterialName = material,
                        Positions = Array.Empty<float>(),
                        Normals = Array.Empty<float>(),
                        TexCoords = Array.Empty<float>(),
                        Indices = Array.Empty<int>(),
                        Transform = System.Numerics.Matrix4x4.Identity
                    }
                }
            };

        [Test]
        public void PickerWritesPackedRgbAndResetRemovesIt()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var root = JsonGffDocument.Parse(
                Encoding.UTF8.GetBytes("""{"__data_type":"UTI "}""")).Root;
            var variables = new VarTable(root);
            var edits = new List<string>();
            var editor = new TintMapEditorViewModel(
                variables,
                (description, mutation) =>
                {
                    edits.Add(description);
                    mutation();
                    return true;
                },
                catalog!);

            editor.Reload(ModelWith("pmo0_footl10"));
            editor.Colors.Select(row => row.Layer)
                .Should().BeEquivalentTo(
                    new[] { TintMapLayerType.Leather1, TintMapLayerType.Leather2 });

            var leather = editor.Colors.Single(row => row.Layer == TintMapLayerType.Leather1);
            leather.Color = Color.FromRgb(12, 34, 56);

            var stored = variables.GetInt(leather.Key);
            stored.Should().NotBeNull();
            TintMapColor.TryFromStoredValue(stored!.Value, out var color).Should().BeTrue();
            color.Should().Be(new TintMapColor(12, 34, 56));
            leather.IsCustom.Should().BeTrue();

            leather.ResetCommand.Execute(null);
            variables.GetInt(leather.Key).Should().BeNull();
            leather.IsCustom.Should().BeFalse();
            edits.Should().HaveCount(2);
        }

        [Test]
        public void ResetRemovesLegacyPaletteOverride()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var root = JsonGffDocument.Parse(
                Encoding.UTF8.GetBytes("""{"__data_type":"UTI "}""")).Root;
            var variables = new VarTable(root);
            var key = TintMapVariable.GetName("pmo0_footl10", TintMapLayerType.Leather1);
            variables.SetInt(key, 42);
            var editor = new TintMapEditorViewModel(
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);

            editor.Reload(ModelWith("pmo0_footl10"));
            var leather = editor.Colors.Single(row => row.Layer == TintMapLayerType.Leather1);
            leather.IsCustom.Should().BeFalse();
            leather.HasOverride.Should().BeTrue();
            leather.Status.Should().Be("Legacy palette override");
            var expectedColor = TintMapPaletteColors.GetColor(TintMapLayerType.Leather1, 41);
            leather.Color.Should().Be(Color.FromRgb(
                expectedColor.Red,
                expectedColor.Green,
                expectedColor.Blue));

            leather.ResetCommand.Execute(null);

            variables.GetInt(key).Should().BeNull();
            leather.HasOverride.Should().BeFalse();
            leather.Status.Should().Be("Standard NWN color");
        }

        [Test]
        public void CreatureEditorFiltersMaterialsWhoseOverridesBelongToEquipment()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var editor = new TintMapEditorViewModel(
                new VarTable(new JsonGffStruct()),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);
            var model = new RenderModel
            {
                Meshes = new[]
                {
                    ModelWith("pmo0_footl10").Meshes.Single(),
                    new RenderMesh
                    {
                        NodeName = "equipped_robe",
                        TextureName = "pmh0_robe010",
                        MaterialName = "pmh0_robe010",
                        Positions = Array.Empty<float>(),
                        Normals = Array.Empty<float>(),
                        TexCoords = Array.Empty<float>(),
                        Indices = Array.Empty<int>(),
                        Transform = System.Numerics.Matrix4x4.Identity,
                        UsesItemTintOverrides = true
                    }
                }
            };

            editor.Reload(model, includeItemOwnedMaterials: false);

            editor.Colors.Should().NotBeEmpty();
            editor.Colors.Select(row => row.MaterialName)
                .Should().OnlyContain(material => material.Equals(
                    "pmo0_footl10", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void CreatureBodyIncludesSemanticSkinFromEquippedBodyMeshesOnly()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var editor = new TintMapEditorViewModel(
                new VarTable(new JsonGffStruct()),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);
            var equippedChest = ModelWith("pmh0_chest189").Meshes.Single();
            equippedChest.UsesItemTintOverrides = true;

            editor.Reload(
                new RenderModel { Meshes = new[] { equippedChest } },
                includeItemOwnedMaterials: false,
                includeCreatureLayersFromItemOwnedMaterials: true);

            editor.Colors.Should().ContainSingle();
            editor.Colors.Single().Layer.Should().Be(TintMapLayerType.Skin);
        }

        [Test]
        public void CreatureAndEquipmentOverridesMergeBySemanticLayerOwnership()
        {
            var skinKey = TintMapVariable.GetName("pmh0_chest189", TintMapLayerType.Skin);
            var clothKey = TintMapVariable.GetName("pmh0_chest189", TintMapLayerType.Cloth1);
            var creature = new Dictionary<string, int>
            {
                [skinKey] = 111,
                [clothKey] = 222
            };
            var item = new Dictionary<string, int>
            {
                [skinKey] = 333,
                [clothKey] = 444
            };

            var merged = TintMapOverrides.MergeCreatureLayers(creature, item);

            merged[skinKey].Should().Be(111);
            merged[clothKey].Should().Be(444);
        }

        [Test]
        public void EquipmentSemanticOverridesAreDiscardedWithoutCreatureOverrides()
        {
            var skinKey = TintMapVariable.GetName("pmh0_chest189", TintMapLayerType.Skin);
            var hairKey = TintMapVariable.GetName("pmh0_chest189", TintMapLayerType.Hair);
            var tattooKey = TintMapVariable.GetName("pmh0_chest189", TintMapLayerType.Tattoo2);
            var clothKey = TintMapVariable.GetName("pmh0_chest189", TintMapLayerType.Cloth1);
            var item = new Dictionary<string, int>
            {
                [skinKey] = 111,
                [hairKey] = 222,
                [tattooKey] = 333,
                [clothKey] = 444
            };

            var merged = TintMapOverrides.MergeCreatureLayers(null, item);

            merged.Should().ContainSingle();
            merged[clothKey].Should().Be(444);
            merged.Should().NotContainKeys(skinKey, hairKey, tattooKey);
        }

        [Test]
        public void PreviewOverridesPreserveAndApplyGlobalColorStateFallbacks()
        {
            var variables = new VarTable(new JsonGffStruct());
            var creatureState = TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Skin);
            var itemState = TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Cloth1);
            var skinColor = new TintMapColor(12, 34, 56).ToStoredValue();
            var clothColor = new TintMapColor(65, 43, 21).ToStoredValue();
            variables.SetInt(creatureState, skinColor);
            variables.SetInt(itemState, clothColor);
            variables.SetInt("UNRELATED", 123);

            var overrides = TintMapOverrides.Read(variables);

            overrides.Should().Contain(creatureState, skinColor);
            overrides.Should().Contain(itemState, clothColor);
            overrides.Should().NotContainKey("UNRELATED");
            TintMapOverrides.GetMaterialColor(
                    overrides,
                    "new_skin_material",
                    TintMapLayerType.Skin)
                .Should().Be(skinColor);
            TintMapOverrides.GetMaterialColor(
                    overrides,
                    "new_cloth_material",
                    TintMapLayerType.Cloth1)
                .Should().Be(clothColor);
        }

        [Test]
        public void ExactMaterialColorTakesPriorityOverGlobalColorState()
        {
            var material = "specific_material";
            var layer = TintMapLayerType.Cloth1;
            var exactKey = TintMapVariable.GetName(material, layer);
            var stateKey = TintMapVariable.GetItemGlobalColorStateName(layer);
            var exact = new TintMapColor(1, 2, 3).ToStoredValue();
            var global = new TintMapColor(4, 5, 6).ToStoredValue();
            var overrides = new Dictionary<string, int>
            {
                [exactKey] = exact,
                [stateKey] = global
            };

            TintMapOverrides.GetMaterialColor(overrides, material, layer).Should().Be(exact);
        }

        [Test]
        public void PerPartPresetMarkerOptsOnlyThatArmorPartOutOfGlobalRgbFallback()
        {
            var variables = new VarTable(new JsonGffStruct());
            var layer = TintMapLayerType.Cloth1;
            var global = new TintMapColor(4, 5, 6).ToStoredValue();
            var torsoMarker = ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                AppearanceArmor.Torso,
                AppearanceArmorColor.Cloth1);
            variables.SetInt(TintMapVariable.GetItemGlobalColorStateName(layer), global);
            variables.SetInt(torsoMarker, 1);

            var overrides = TintMapOverrides.Read(variables);

            overrides.Should().Contain(torsoMarker, 1,
                "the preview snapshot must retain the per-part preset intent");
            TintMapOverrides.GetMaterialColor(
                    overrides,
                    "new_material",
                    layer,
                    AppearanceArmor.Torso)
                .Should().Be(0,
                    "the torso explicitly chose its ordinary palette color");
            TintMapOverrides.GetMaterialColor(
                    overrides,
                    "new_material",
                    layer,
                    AppearanceArmor.RightHand)
                .Should().Be(global,
                    "a sibling part without a marker must still inherit the global RGB tint");
        }

        [Test]
        public void ExactMaterialTintStillWinsOverPerPartPresetMarker()
        {
            var variables = new VarTable(new JsonGffStruct());
            var material = "specific_material";
            var layer = TintMapLayerType.Cloth1;
            var exact = new TintMapColor(1, 2, 3).ToStoredValue();
            variables.SetInt(TintMapVariable.GetName(material, layer), exact);
            variables.SetInt(
                ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                    AppearanceArmor.Torso,
                    AppearanceArmorColor.Cloth1),
                1);

            TintMapOverrides.GetMaterialColor(
                    variables,
                    material,
                    layer,
                    AppearanceArmor.Torso)
                .Should().Be(exact);
        }

        [Test]
        public void TintRowDisplaysPersistedSemanticColorForNewMaterial()
        {
            var variables = new VarTable(new JsonGffStruct());
            var color = new TintMapColor(12, 34, 56);
            variables.SetInt(
                TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Skin),
                color.ToStoredValue());

            var row = new TintMapColorRowViewModel(
                "new_skin_material",
                TintMapLayerType.Skin,
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                null);

            row.IsCustom.Should().BeTrue();
            row.HasOverride.Should().BeTrue();
            row.Color.Should().Be(Color.FromRgb(color.Red, color.Green, color.Blue));
        }

        [Test]
        public void ResetMaterialInheritedFromGlobalItemTintRestoresOnlyItsStockPaletteColor()
        {
            var variables = new VarTable(new JsonGffStruct());
            var layer = TintMapLayerType.Cloth1;
            var globalKey = TintMapVariable.GetItemGlobalColorStateName(layer);
            var globalColor = new TintMapColor(12, 34, 56).ToStoredValue();
            variables.SetInt(globalKey, globalColor);
            var row = new TintMapColorRowViewModel(
                "item_material",
                layer,
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                null,
                standardPaletteColorId: 37);

            row.Color.Should().Be(Color.FromRgb(12, 34, 56));
            row.ResetCommand.Execute(null);

            variables.GetInt(row.Key).Should().Be(38,
                "palette overrides are stored one-based and must mask the inherited global tint");
            variables.GetInt(globalKey).Should().Be(globalColor,
                "resetting one material must not discard the item-wide tint from sibling materials");
            TintMapOverrides.GetMaterialColor(variables, row.MaterialName, layer).Should().Be(38);
            row.IsCustom.Should().BeFalse();
            row.Color.Should().Be(Color.FromRgb(
                TintMapPaletteColors.GetColor(layer, 37).Red,
                TintMapPaletteColors.GetColor(layer, 37).Green,
                TintMapPaletteColors.GetColor(layer, 37).Blue));
        }

        [Test]
        public void TintEditorCarriesArmorPartContextIntoRowsThatOptOutOfGlobalRgb()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var variables = new VarTable(new JsonGffStruct());
            var layer = TintMapLayerType.Cloth1;
            var material = "helm_004";
            var exactKey = TintMapVariable.GetName(material, layer);
            variables.SetInt(
                TintMapVariable.GetItemGlobalColorStateName(layer),
                new TintMapColor(12, 34, 56).ToStoredValue());
            variables.SetInt(
                ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                    AppearanceArmor.Torso,
                    AppearanceArmorColor.Cloth1),
                1);
            variables.SetInt(exactKey, new TintMapColor(90, 80, 70).ToStoredValue());
            var editor = new TintMapEditorViewModel(
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);
            var model = ItemOwnedModelWith(material);
            model.Meshes.Single().ArmorPart = AppearanceArmor.Torso;

            editor.Reload(model, includeNonItemOwnedMaterials: false);

            var row = editor.Colors.Single(entry =>
                entry.MaterialName == material && entry.Layer == layer);
            row.ArmorPart.Should().Be(AppearanceArmor.Torso);
            row.IsCustom.Should().BeTrue("the exact material tint still wins");

            row.ResetCommand.Execute(null);

            variables.GetInt(exactKey).Should().BeNull(
                "the per-part preset marker already masks the item-wide RGB tint");
            row.IsCustom.Should().BeFalse();
            row.HasOverride.Should().BeFalse();
        }

        [Test]
        public void TintEditorUsesMatchingMeshPaletteWhenResettingAnInheritedGlobalItemTint()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var variables = new VarTable(new JsonGffStruct());
            var layer = TintMapLayerType.Leather1;
            variables.SetInt(
                TintMapVariable.GetItemGlobalColorStateName(layer),
                new TintMapColor(65, 43, 21).ToStoredValue());
            var model = ModelWith("pmo0_footl10");
            model.Meshes.Single().LayerColorIndices = new Dictionary<int, int>
            {
                [(int)layer] = 73
            };
            var editor = new TintMapEditorViewModel(
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);

            editor.Reload(model);
            var row = editor.Colors.Single(color => color.Layer == layer);
            row.ResetCommand.Execute(null);

            variables.GetInt(row.Key).Should().Be(74);
        }

        [Test]
        public void TintEditorRefreshesStockPaletteColorWhenTheModelKeysStayTheSame()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var variables = new VarTable(new JsonGffStruct());
            var layer = TintMapLayerType.Leather1;
            variables.SetInt(
                TintMapVariable.GetItemGlobalColorStateName(layer),
                new TintMapColor(65, 43, 21).ToStoredValue());
            var model = ModelWith("pmo0_footl10");
            model.Meshes.Single().LayerColorIndices = new Dictionary<int, int>
            {
                [(int)layer] = 12
            };
            var editor = new TintMapEditorViewModel(
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);
            editor.Reload(model);

            model.Meshes.Single().LayerColorIndices = new Dictionary<int, int>
            {
                [(int)layer] = 91
            };
            editor.Reload(model);
            var row = editor.Colors.Single(color => color.Layer == layer);
            row.ResetCommand.Execute(null);

            variables.GetInt(row.Key).Should().Be(92,
                "the retained tint row must follow a newer stock dye on the same model material");
        }

        [Test]
        public void CatalogFindsKnownTintMaterialThroughMeshTextureFallback()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var model = new RenderModel
            {
                Meshes =
                [
                    new RenderMesh
                    {
                        NodeName = "stock_cloak",
                        TextureName = "cloak_102",
                        MaterialName = string.Empty,
                        Positions = Array.Empty<float>(),
                        Normals = Array.Empty<float>(),
                        TexCoords = Array.Empty<float>(),
                        Indices = Array.Empty<int>(),
                        Transform = System.Numerics.Matrix4x4.Identity
                    }
                ]
            };

            var materials = catalog!.FindMaterials(model);

            materials.Should().ContainSingle(material =>
                material.Resref.Equals("cloak_102", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public async Task LegacyBodyOverrideRemainsPresetInsteadOfBecomingMisleadingCustomRgb()
        {
            var creature = new ModuleWorkspace(CorpusLocator.ModuleDirectory)
                .LoadBlueprint(ResourceType.Utc, "agr_guildmaster")
                .Document.Root;
            var store = new SWLOR.Toolset.Domain.Editors.Creatures.CreatureValueStore(creature);
            static bool Edit(string _, Action mutation)
            {
                mutation();
                return true;
            }

            var key = TintMapVariable.GetName("pmh0_head038", TintMapLayerType.Skin);
            store.Locals.SetInt(key, 42);
            var tintRows = new[]
            {
                new TintMapColorRowViewModel(
                    "pmh0_head038",
                    TintMapLayerType.Skin,
                    store.Locals,
                    Edit,
                    null)
            };
            var body = new CreatureBodyPartsViewModel(
                store,
                Edit,
                id => new AppearanceRow(id, "DYNAMIC_TEST", "Dynamic Test", "P", "H", null),
                null,
                null,
                () => { });
            body.SetTintMapRows(tintRows);
            await body.EnsureLoadedAsync();

            var skin = body.Colors.Single(color => color.Label == "Skin");
            skin.HasOverride.Should().BeTrue();
            skin.Palette.IsUsingCustomColor.Should().BeFalse();
            tintRows.Single().Status.Should().Be("Legacy palette override");
        }

        [Test]
        public async Task CreatureBodyColorCombinesPresetAndCustomRgbForOneSemanticChannel()
        {
            var creature = new ModuleWorkspace(CorpusLocator.ModuleDirectory)
                .LoadBlueprint(ResourceType.Utc, "agr_guildmaster")
                .Document.Root;
            var store = new SWLOR.Toolset.Domain.Editors.Creatures.CreatureValueStore(creature);
            static bool Edit(string _, Action mutation)
            {
                mutation();
                return true;
            }
            var geometryChangeCount = 0;
            var colorChangeCount = 0;

            var tintRows = new[]
            {
                new TintMapColorRowViewModel(
                    "pmh0_head038",
                    TintMapLayerType.Skin,
                    store.Locals,
                    Edit,
                    null)
            };
            var body = new CreatureBodyPartsViewModel(
                store,
                Edit,
                id => new AppearanceRow(id, "DYNAMIC_TEST", "Dynamic Test", "P", "H", null),
                null,
                null,
                () => geometryChangeCount++,
                () => colorChangeCount++);
            body.SetTintMapRows(tintRows);
            await body.EnsureLoadedAsync();

            var skin = body.Colors.Single(color => color.Label == "Skin");
            skin.HasCustomTint.Should().BeTrue();
            skin.CustomColor = Color.FromRgb(12, 34, 56);

            var key = TintMapVariable.GetName("pmh0_head038", TintMapLayerType.Skin);
            var stateKey = TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Skin);
            TintMapColor.TryFromStoredValue(store.Locals.GetInt(key)!.Value, out var custom)
                .Should().BeTrue();
            custom.Should().Be(new TintMapColor(12, 34, 56));
            store.Locals.GetInt(stateKey).Should().Be(custom.ToStoredValue(),
                "the semantic control represents a global tint intent for future materials");
            skin.HasOverride.Should().BeTrue();
            colorChangeCount.Should().Be(1);
            geometryChangeCount.Should().Be(0,
                "RGB edits must recolor the retained model instead of rebuilding preview geometry");

            skin.Palette.Number = 12;

            store.GetInteger(
                    SWLOR.Toolset.Domain.Editors.Behaviors.BehaviorFieldStorage.Field,
                    "Color_Skin")
                .Should().Be(12);
            store.Locals.GetInt(key).Should().BeNull(
                "choosing a preset replaces the custom RGB value for the same color channel");
            store.Locals.GetInt(stateKey).Should().BeNull(
                "choosing a preset clears the persisted global tint intent");
            skin.HasOverride.Should().BeFalse();
            colorChangeCount.Should().Be(2);
            geometryChangeCount.Should().Be(0,
                "palette changes also preserve the current preview camera");
        }

        [Test]
        public async Task CreatureBodyColorFollowsReplacementMaterialsWithoutLeavingStaleValues()
        {
            var creature = new ModuleWorkspace(CorpusLocator.ModuleDirectory)
                .LoadBlueprint(ResourceType.Utc, "agr_guildmaster")
                .Document.Root;
            var store = new SWLOR.Toolset.Domain.Editors.Creatures.CreatureValueStore(creature);
            static bool Edit(string _, Action mutation)
            {
                mutation();
                return true;
            }
            var geometryChangeCount = 0;
            var colorChangeCount = 0;
            var oldKey = TintMapVariable.GetName("pmh0_head038", TintMapLayerType.Skin);
            var newKey = TintMapVariable.GetName("pmh0_head221", TintMapLayerType.Skin);
            var original = new TintMapColor(12, 34, 56);
            store.Locals.SetInt(oldKey, original.ToStoredValue());

            var oldRows = new[]
            {
                new TintMapColorRowViewModel(
                    "pmh0_head038", TintMapLayerType.Skin, store.Locals, Edit, null)
            };
            var body = new CreatureBodyPartsViewModel(
                store,
                Edit,
                id => new AppearanceRow(id, "DYNAMIC_TEST", "Dynamic Test", "P", "H", null),
                null,
                null,
                () => geometryChangeCount++,
                () => colorChangeCount++);
            body.SetTintMapRows(oldRows);
            await body.EnsureLoadedAsync();

            var newRows = new[]
            {
                new TintMapColorRowViewModel(
                    "pmh0_head221", TintMapLayerType.Skin, store.Locals, Edit, null)
            };
            body.SetTintMapRows(newRows);

            TintMapColor.TryFromStoredValue(store.Locals.GetInt(newKey)!.Value, out var carried)
                .Should().BeTrue();
            carried.Should().Be(original,
                "a replacement body part must immediately inherit its semantic custom color");

            var skin = body.Colors.Single(color => color.Label == "Skin");
            skin.CustomColor = Color.FromRgb(65, 43, 21);

            foreach (var key in new[] { oldKey, newKey })
            {
                TintMapColor.TryFromStoredValue(store.Locals.GetInt(key)!.Value, out var updated)
                    .Should().BeTrue();
                updated.Should().Be(new TintMapColor(65, 43, 21),
                    "inactive materials must not resurrect an older semantic color");
            }

            skin.Palette.Number = 9;

            store.Locals.GetInt(oldKey).Should().BeNull();
            store.Locals.GetInt(newKey).Should().BeNull();
            geometryChangeCount.Should().Be(0,
                "semantic tint transfers and edits must retain the preview geometry and camera");
            colorChangeCount.Should().Be(2);
        }

        [Test]
        public async Task PartialSemanticCustomColorsDoNotSpreadAcrossReplacementMaterials()
        {
            var creature = new ModuleWorkspace(CorpusLocator.ModuleDirectory)
                .LoadBlueprint(ResourceType.Utc, "agr_guildmaster")
                .Document.Root;
            var store = new CreatureValueStore(creature);
            static bool Edit(string _, Action mutation)
            {
                mutation();
                return true;
            }

            var customKey = TintMapVariable.GetName("pmh0_head038", TintMapLayerType.Skin);
            var presetKey = TintMapVariable.GetName("pmh0_handl001", TintMapLayerType.Skin);
            var replacementKey = TintMapVariable.GetName("pmh0_head221", TintMapLayerType.Skin);
            var stateKey = TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Skin);
            store.Locals.SetInt(customKey, new TintMapColor(12, 34, 56).ToStoredValue());
            store.Locals.SetInt(stateKey, new TintMapColor(90, 80, 70).ToStoredValue());
            var body = new CreatureBodyPartsViewModel(
                store,
                Edit,
                id => new AppearanceRow(id, "DYNAMIC_TEST", "Dynamic Test", "P", "H", null),
                null,
                null,
                () => { });
            var initialRows = new[]
            {
                new TintMapColorRowViewModel(
                    "pmh0_head038", TintMapLayerType.Skin, store.Locals, Edit, null),
                new TintMapColorRowViewModel(
                    "pmh0_handl001", TintMapLayerType.Skin, store.Locals, Edit, null)
            };
            body.SetTintMapRows(initialRows);
            await body.EnsureLoadedAsync();

            initialRows[0].Color = Color.FromRgb(13, 34, 56);
            store.Locals.GetInt(stateKey).Should().BeNull(
                "a direct per-material edit is no longer a global semantic tint intent");

            body.SetTintMapRows(new[]
            {
                new TintMapColorRowViewModel(
                    "pmh0_head221", TintMapLayerType.Skin, store.Locals, Edit, null),
                new TintMapColorRowViewModel(
                    "pmh0_handl001", TintMapLayerType.Skin, store.Locals, Edit, null)
            });

            store.Locals.GetInt(customKey).Should().NotBeNull();
            store.Locals.GetInt(presetKey).Should().BeNull();
            store.Locals.GetInt(replacementKey).Should().BeNull(
                "one custom material must not be mistaken for a global semantic tint");
        }

        [Test]
        public async Task CreatureBodyColorCarryCoalescesWithThePartEditThatTriggeredIt()
        {
            var creature = new ModuleWorkspace(CorpusLocator.ModuleDirectory)
                .LoadBlueprint(ResourceType.Utc, "agr_guildmaster")
                .Document.Root;
            var store = new CreatureValueStore(creature);
            var origin = new NoOpDocumentEdit("change head");
            IDocumentEdit? currentAppliedEdit = null;
            IDocumentEdit? coalescedOrigin = null;
            var ordinaryEdits = new List<string>();
            bool Edit(string description, Action mutation)
            {
                ordinaryEdits.Add(description);
                mutation();
                if (description == "Change Head")
                    currentAppliedEdit = origin;
                return true;
            }

            var oldKey = TintMapVariable.GetName("pmh0_head038", TintMapLayerType.Skin);
            var newKey = TintMapVariable.GetName("pmh0_head221", TintMapLayerType.Skin);
            store.Locals.SetInt(oldKey, new TintMapColor(12, 34, 56).ToStoredValue());
            var body = new CreatureBodyPartsViewModel(
                store,
                Edit,
                id => new AppearanceRow(id, "DYNAMIC_TEST", "Dynamic Test", "P", "H", null),
                null,
                null,
                () => { },
                captureCoalesceOrigin: () => currentAppliedEdit,
                runCoalescedEdit: (capturedOrigin, _, mutation) =>
                {
                    coalescedOrigin = capturedOrigin;
                    mutation();
                    return true;
                });
            body.SetTintMapRows(new[]
            {
                new TintMapColorRowViewModel(
                    "pmh0_head038", TintMapLayerType.Skin, store.Locals, Edit, null)
            });
            await body.EnsureLoadedAsync();

            body.Structure.Single(cell => cell.Label == "Head").Number = 221;
            body.SetTintMapRows(new[]
            {
                new TintMapColorRowViewModel(
                    "pmh0_head221", TintMapLayerType.Skin, store.Locals, Edit, null)
            });

            coalescedOrigin.Should().BeSameAs(origin,
                "automatic tint migration must undo with its originating body-part edit");
            ordinaryEdits.Should().NotContain("Carry custom body colors to replacement models");
            store.Locals.GetInt(newKey).Should().NotBeNull();
        }

        [Test]
        public async Task CreatureBodyColorCarryCoalescesWithAnInterveningTintEdit()
        {
            var creature = new ModuleWorkspace(CorpusLocator.ModuleDirectory)
                .LoadBlueprint(ResourceType.Utc, "agr_guildmaster")
                .Document.Root;
            var store = new CreatureValueStore(creature);
            var partOrigin = new NoOpDocumentEdit("change head");
            var tintOrigin = new NoOpDocumentEdit("change skin tint");
            IDocumentEdit? currentAppliedEdit = null;
            IDocumentEdit? coalescedOrigin = null;
            bool Edit(string description, Action mutation)
            {
                mutation();
                currentAppliedEdit = description == "Change Head" ? partOrigin : tintOrigin;
                return true;
            }

            var oldKey = TintMapVariable.GetName("pmh0_head038", TintMapLayerType.Skin);
            var newKey = TintMapVariable.GetName("pmh0_head221", TintMapLayerType.Skin);
            store.Locals.SetInt(oldKey, new TintMapColor(12, 34, 56).ToStoredValue());
            var body = new CreatureBodyPartsViewModel(
                store,
                Edit,
                id => new AppearanceRow(id, "DYNAMIC_TEST", "Dynamic Test", "P", "H", null),
                null,
                null,
                () => { },
                () => { },
                captureCoalesceOrigin: () => currentAppliedEdit,
                runCoalescedEdit: (capturedOrigin, _, mutation) =>
                {
                    coalescedOrigin = capturedOrigin;
                    mutation();
                    return true;
                });
            body.SetTintMapRows(new[]
            {
                new TintMapColorRowViewModel(
                    "pmh0_head038", TintMapLayerType.Skin, store.Locals, Edit, null)
            });
            await body.EnsureLoadedAsync();

            body.Structure.Single(cell => cell.Label == "Head").Number = 221;
            body.Colors.Single(color => color.Label == "Skin").CustomColor =
                Color.FromRgb(65, 43, 21);
            body.SetTintMapRows(new[]
            {
                new TintMapColorRowViewModel(
                    "pmh0_head221", TintMapLayerType.Skin, store.Locals, Edit, null)
            });

            coalescedOrigin.Should().BeSameAs(tintOrigin,
                "the replacement key belongs to the newer tint transaction when it intervenes");
            TintMapColor.TryFromStoredValue(store.Locals.GetInt(newKey)!.Value, out var carried)
                .Should().BeTrue();
            carried.Should().Be(new TintMapColor(65, 43, 21));
        }

        [Test]
        public void ItemEditorFiltersMannequinMaterialsWhenModelIdentifiesItemOwnedMeshes()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var editor = new TintMapEditorViewModel(
                new VarTable(new JsonGffStruct()),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);
            var itemMesh = ModelWith("pmh0_robe010").Meshes.Single();
            itemMesh.UsesItemTintOverrides = true;
            var model = new RenderModel
            {
                Meshes = new[]
                {
                    ModelWith("pmo0_footl10").Meshes.Single(),
                    itemMesh
                }
            };

            editor.Reload(model, includeNonItemOwnedMaterials: false);

            editor.Colors.Should().NotBeEmpty();
            editor.Colors.Select(row => row.MaterialName)
                .Should().OnlyContain(material => material.Equals(
                    "pmh0_robe010", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void ItemEditorExcludesCreatureOwnedLayersFromItemMeshes()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var editor = new TintMapEditorViewModel(
                new VarTable(new JsonGffStruct()),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);
            var itemMesh = ModelWith("helm_004").Meshes.Single();
            itemMesh.UsesItemTintOverrides = true;

            editor.Reload(
                new RenderModel { Meshes = new[] { itemMesh } },
                includeNonItemOwnedMaterials: false);

            editor.Colors.Select(row => row.Layer).Should().BeEquivalentTo(new[]
            {
                TintMapLayerType.Cloth1,
                TintMapLayerType.Leather1,
                TintMapLayerType.Leather2
            });
            editor.Colors.Select(row => row.Layer)
                .Should().OnlyContain(layer => !TintMapVariable.IsCreatureColorLayer(layer));
        }

        [Test]
        public void ItemCustomColorFollowsAReplacementMaterialAndRemovesItsStaleKey()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var variables = new VarTable(new JsonGffStruct());
            var editor = new TintMapEditorViewModel(
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);
            var oldModel = ModelWith("helm_004");
            oldModel.Meshes.Single().UsesItemTintOverrides = true;
            var newModel = ModelWith("helm_005");
            newModel.Meshes.Single().UsesItemTintOverrides = true;
            var oldKey = TintMapVariable.GetName("helm_004", TintMapLayerType.Cloth1);
            var newKey = TintMapVariable.GetName("helm_005", TintMapLayerType.Cloth1);

            editor.Reload(
                oldModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);
            editor.Colors.Single(row => row.Layer == TintMapLayerType.Cloth1).Color =
                Color.FromRgb(12, 34, 56);

            editor.Reload(
                null,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);
            editor.Reload(
                newModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);

            TintMapColor.TryFromStoredValue(variables.GetInt(newKey)!.Value, out var carried)
                .Should().BeTrue();
            carried.Should().Be(new TintMapColor(12, 34, 56));
            variables.GetInt(oldKey).Should().BeNull(
                "returning to the old model must not resurrect its obsolete custom color");
            editor.Colors.Single(row => row.Key == newKey).IsCustom.Should().BeTrue();
        }

        [Test]
        public void ItemPaletteOptOutFollowsAReplacementMaterialWhileGlobalTintRemains()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var variables = new VarTable(new JsonGffStruct());
            var editor = new TintMapEditorViewModel(
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);
            var layer = TintMapLayerType.Cloth1;
            var oldModel = ItemOwnedModelWith("helm_004");
            var newModel = ItemOwnedModelWith("helm_005");
            var oldKey = TintMapVariable.GetName("helm_004", layer);
            var newKey = TintMapVariable.GetName("helm_005", layer);
            var paletteOverride = 38;
            variables.SetInt(
                TintMapVariable.GetItemGlobalColorStateName(layer),
                new TintMapColor(12, 34, 56).ToStoredValue());
            variables.SetInt(oldKey, paletteOverride);

            editor.Reload(
                oldModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);
            editor.Reload(
                null,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);
            editor.Reload(
                newModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);

            variables.GetInt(newKey).Should().Be(paletteOverride,
                "the replacement material must retain its explicit stock-color opt-out");
            variables.GetInt(oldKey).Should().BeNull();
            editor.Colors.Single(row => row.Key == newKey).HasOverride.Should().BeTrue();
            editor.Colors.Single(row => row.Key == newKey).IsCustom.Should().BeFalse();
        }

        [Test]
        public void PresetEditDuringItemModelLoadCancelsTheOlderCustomColorCarry()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var variables = new VarTable(new JsonGffStruct());
            var editor = new TintMapEditorViewModel(
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);
            var oldModel = ModelWith("helm_004");
            oldModel.Meshes.Single().UsesItemTintOverrides = true;
            var newModel = ModelWith("helm_005");
            newModel.Meshes.Single().UsesItemTintOverrides = true;
            var oldKey = TintMapVariable.GetName("helm_004", TintMapLayerType.Cloth1);
            var newKey = TintMapVariable.GetName("helm_005", TintMapLayerType.Cloth1);

            editor.Reload(
                oldModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);
            editor.Colors.Single(row => row.Layer == TintMapLayerType.Cloth1).Color =
                Color.FromRgb(12, 34, 56);
            editor.Reload(
                null,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);

            variables.Remove(oldKey);
            editor.Reload(
                newModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);

            variables.GetInt(oldKey).Should().BeNull();
            variables.GetInt(newKey).Should().BeNull(
                "a later preset choice must win over the custom tint captured before model loading");
        }

        [Test]
        public void PresetEditDuringItemModelLoadCancelsOnlyItsTintLayer()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var variables = new VarTable(new JsonGffStruct());
            var editor = new TintMapEditorViewModel(
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);
            var oldModel = ItemOwnedModelWith("helm_004");
            var newModel = ItemOwnedModelWith("helm_005");
            var oldCloth = TintMapVariable.GetName("helm_004", TintMapLayerType.Cloth1);
            var newCloth = TintMapVariable.GetName("helm_005", TintMapLayerType.Cloth1);
            var oldLeather = TintMapVariable.GetName("helm_004", TintMapLayerType.Leather1);
            var newLeather = TintMapVariable.GetName("helm_005", TintMapLayerType.Leather1);

            editor.Reload(
                oldModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);
            editor.Colors.Single(row => row.Layer == TintMapLayerType.Cloth1).Color =
                Color.FromRgb(12, 34, 56);
            editor.Colors.Single(row => row.Layer == TintMapLayerType.Leather1).Color =
                Color.FromRgb(65, 43, 21);
            editor.Reload(
                null,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);

            variables.Remove(oldCloth);
            editor.Reload(
                newModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);

            variables.GetInt(newCloth).Should().BeNull(
                "the changed Cloth 1 preset must cancel only Cloth 1 carry");
            variables.GetInt(oldCloth).Should().BeNull();
            variables.GetInt(oldLeather).Should().BeNull(
                "the untouched Leather 1 layer must still clean up its obsolete source key");
            TintMapColor.TryFromStoredValue(variables.GetInt(newLeather)!.Value, out var leather)
                .Should().BeTrue();
            leather.Should().Be(new TintMapColor(65, 43, 21));
        }

        [AvaloniaTest]
        public void MannequinSexChangeCarriesCustomColorsToTheVisibleWearerVariant()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var item = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Uti,
                "gender_tint",
                "Gender Tint")).Root;
            var variables = new ItemValueStore(item).Locals;
            var maleKey = TintMapVariable.GetName("pmh0_robe030", TintMapLayerType.Cloth1);
            var femaleKey = TintMapVariable.GetName("pfh0_robe149", TintMapLayerType.Cloth1);
            variables.SetInt(maleKey, new TintMapColor(12, 34, 56).ToStoredValue());

            using var editor = new ItemEditorViewModel(
                item,
                "gender_tint",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                resolveModel: (_, female) => ItemOwnedModelWith(
                    female ? "pfh0_robe149" : "pmh0_robe030"),
                tintMapCatalog: catalog);
            DrainUntil(() => !editor.IsModelPreviewLoading);

            editor.TintMapEditor!.Colors.Should().Contain(row => row.Key == maleKey && row.IsCustom);

            editor.PreviewFemale = true;
            Dispatcher.UIThread.RunJobs();
            DrainUntil(() => !editor.IsModelPreviewLoading);

            variables.GetInt(maleKey).Should().BeNull(
                "the hidden wearer variant must not retain an obsolete material-specific key");
            TintMapColor.TryFromStoredValue(variables.GetInt(femaleKey)!.Value, out var carried)
                .Should().BeTrue();
            carried.Should().Be(new TintMapColor(12, 34, 56));
            editor.TintMapEditor.Colors.Should().Contain(row =>
                row.Key == femaleKey && row.IsCustom && row.Color == Color.FromRgb(12, 34, 56));
        }

        [AvaloniaTest]
        public void MannequinSexChangeCarriesPaletteOptOutToTheVisibleWearerVariant()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var item = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Uti,
                "gender_pal_tint",
                "Gender Palette Tint")).Root;
            var variables = new ItemValueStore(item).Locals;
            var layer = TintMapLayerType.Cloth1;
            var maleKey = TintMapVariable.GetName("pmh0_robe030", layer);
            var femaleKey = TintMapVariable.GetName("pfh0_robe149", layer);
            var paletteOverride = 38;
            variables.SetInt(
                TintMapVariable.GetItemGlobalColorStateName(layer),
                new TintMapColor(12, 34, 56).ToStoredValue());
            variables.SetInt(maleKey, paletteOverride);

            using var editor = new ItemEditorViewModel(
                item,
                "gender_pal_tint",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                resolveModel: (_, female) => ItemOwnedModelWith(
                    female ? "pfh0_robe149" : "pmh0_robe030"),
                tintMapCatalog: catalog);
            DrainUntil(() => !editor.IsModelPreviewLoading);

            editor.PreviewFemale = true;
            Dispatcher.UIThread.RunJobs();
            DrainUntil(() => !editor.IsModelPreviewLoading);

            variables.GetInt(maleKey).Should().BeNull();
            variables.GetInt(femaleKey).Should().Be(paletteOverride);
            editor.TintMapEditor!.Colors.Should().Contain(row =>
                row.Key == femaleKey && row.HasOverride && !row.IsCustom);
        }

        [AvaloniaTest]
        public void ItemPreviewCapturesModelEditOriginBeforeDeferredRebuild()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var item = new ModuleWorkspace(CorpusLocator.ModuleDirectory)
                .LoadBlueprint(ResourceType.Uti, "adren_harness")
                .Document.Root;
            var originalTorso = ItemAppearanceValues.Read(item, ItemAppearanceFieldNames.Torso);
            var modelOrigin = new NoOpDocumentEdit("change torso");
            var unrelatedOrigin = new NoOpDocumentEdit("unrelated edit");
            IDocumentEdit? currentAppliedEdit = null;
            IDocumentEdit? coalescedOrigin = null;
            bool Edit(string description, Action mutation)
            {
                mutation();
                currentAppliedEdit = description == "Set Torso" ? modelOrigin : unrelatedOrigin;
                return true;
            }

            using var editor = new ItemEditorViewModel(
                item,
                "adren_harness",
                Edit,
                baseItemRows: baseItem => baseItem == 16
                    ? new BaseItemRow(16, "armor", 3)
                    : null,
                baseItemIcons: baseItem => baseItem == 16
                    ? new BaseItemIconRow(16, 3, "AArCl", "gifp")
                    : null,
                textureExists: _ => true,
                resolveModel: (snapshot, _) => ItemOwnedModelWith(
                    ItemAppearanceValues.Read(snapshot, ItemAppearanceFieldNames.Torso) == originalTorso
                        ? "helm_004"
                        : "helm_005"),
                tintMapCatalog: catalog,
                captureCoalesceOrigin: () => currentAppliedEdit,
                runCoalescedEdit: (origin, _, mutation) =>
                {
                    coalescedOrigin = origin;
                    mutation();
                    return true;
                });
            DrainUntil(() => !editor.IsModelPreviewLoading);
            editor.TintMapEditor!.Colors.Single(row => row.Layer == TintMapLayerType.Cloth1).Color =
                Color.FromRgb(12, 34, 56);

            editor.Appearance!.Armor!.Torso.Number = originalTorso + 1;
            editor.Appearance.Armor.Cloth1.Number =
                ((editor.Appearance.Armor.Cloth1.Number ?? 0) + 1) % 176;
            currentAppliedEdit.Should().BeSameAs(unrelatedOrigin,
                "the later non-geometry appearance edit must be the active transaction");
            Dispatcher.UIThread.RunJobs();
            DrainUntil(() => !editor.IsModelPreviewLoading);

            coalescedOrigin.Should().BeSameAs(modelOrigin,
                "the queued rebuild must retain the appearance transaction captured synchronously");
        }

        [AvaloniaTest]
        public async Task CreatureModelReloadRetainsSemanticColorsThroughItsEmptyLoadingScene()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var creature = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc,
                "semantic_reload",
                "Semantic Reload")).Root;
            var variables = new CreatureValueStore(creature).Locals;
            using var secondStarted = new ManualResetEventSlim();
            using var releaseSecond = new ManualResetEventSlim();
            var calls = 0;
            using var editor = new CreatureEditorViewModel(
                creature,
                Path.Combine(CorpusLocator.ModuleDirectory, "utc", "semantic_reload.utc.json"),
                "semantic_reload",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                null,
                null,
                null,
                _ =>
                {
                    if (Interlocked.Increment(ref calls) == 1)
                        return ModelWith("pmh0_head038");

                    secondStarted.Set();
                    releaseSecond.Wait();
                    return ModelWith("pmh0_head220");
                },
                id => new AppearanceRow(id, "DYNAMIC_TEST", "Dynamic Test", "P", "H", null),
                null,
                tintMapCatalog: catalog);
            DrainUntil(() => !editor.IsModelPreviewLoading);
            await editor.BodyParts.EnsureLoadedAsync();

            var oldKey = TintMapVariable.GetName("pmh0_head038", TintMapLayerType.Skin);
            var newKey = TintMapVariable.GetName("pmh0_head220", TintMapLayerType.Skin);
            var expected = Color.FromRgb(12, 34, 56);
            editor.BodyParts.Colors.Single(color => color.Label == "Skin").CustomColor = expected;

            var updatePreview = typeof(CreatureEditorViewModel).GetMethod(
                "UpdatePreviewScene",
                BindingFlags.Instance | BindingFlags.NonPublic);
            updatePreview.Should().NotBeNull();
            try
            {
                updatePreview!.Invoke(editor, null);
                secondStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

                editor.TintMapEditor!.Colors.Should().Contain(row => row.Key == oldKey,
                    "the temporary empty scene must retain the source semantic rows");
                editor.BodyParts.Colors.Single(color => color.Label == "Skin").CustomColor
                    .Should().Be(expected);
            }
            finally
            {
                releaseSecond.Set();
            }

            DrainUntil(() => !editor.IsModelPreviewLoading);

            TintMapColor.TryFromStoredValue(variables.GetInt(newKey)!.Value, out var carried)
                .Should().BeTrue();
            carried.Should().Be(new TintMapColor(12, 34, 56),
                "the resolved replacement material must receive the retained semantic color");
        }

        [Test]
        public void ItemModelReplacementWithoutLayerRemovesItsStaleCustomColor()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var variables = new VarTable(new JsonGffStruct());
            var editor = new TintMapEditorViewModel(
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);
            var oldModel = ModelWith("helm_004");
            oldModel.Meshes.Single().UsesItemTintOverrides = true;
            var replacementWithoutCloth = ModelWith("pmo0_footl10");
            replacementWithoutCloth.Meshes.Single().UsesItemTintOverrides = true;
            var oldKey = TintMapVariable.GetName("helm_004", TintMapLayerType.Cloth1);

            editor.Reload(
                oldModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);
            editor.Colors.Single(row => row.Layer == TintMapLayerType.Cloth1).Color =
                Color.FromRgb(12, 34, 56);

            editor.Reload(
                null,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);
            editor.Reload(
                replacementWithoutCloth,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);

            variables.GetInt(oldKey).Should().BeNull(
                "a destinationless layer must not resurrect when the old model is selected again");
        }

        [Test]
        public void ItemModelReplacementUsesOriginatingEditForAutomaticColorCarry()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var variables = new VarTable(new JsonGffStruct());
            var origin = new NoOpDocumentEdit("change model");
            IDocumentEdit? coalescedOrigin = null;
            var editor = new TintMapEditorViewModel(
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!,
                runCoalescedEdit: (capturedOrigin, _, mutation) =>
                {
                    coalescedOrigin = capturedOrigin;
                    mutation();
                    return true;
                });
            var oldModel = ModelWith("helm_004");
            var newModel = ModelWith("helm_005");
            oldModel.Meshes.Single().UsesItemTintOverrides = true;
            newModel.Meshes.Single().UsesItemTintOverrides = true;

            editor.Reload(oldModel, includeNonItemOwnedMaterials: false);
            editor.Colors.Single(row => row.Layer == TintMapLayerType.Cloth1).Color =
                Color.FromRgb(12, 34, 56);
            editor.Reload(
                null,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true,
                coalesceOrigin: origin);
            editor.Reload(
                newModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);

            coalescedOrigin.Should().BeSameAs(origin);
            variables.GetInt(TintMapVariable.GetName("helm_005", TintMapLayerType.Cloth1))
                .Should().NotBeNull();
        }

        [Test]
        public void RejectedItemColorCarryIsNotReusedByALaterReplacement()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var variables = new VarTable(new JsonGffStruct());
            var firstOrigin = new NoOpDocumentEdit("replace first model");
            var secondOrigin = new NoOpDocumentEdit("replace second model");
            IDocumentEdit? appliedOrigin = null;
            var carryAttempts = 0;
            var editor = new TintMapEditorViewModel(
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!,
                runCoalescedEdit: (origin, _, mutation) =>
                {
                    carryAttempts++;
                    if (carryAttempts == 1)
                        return false;

                    appliedOrigin = origin;
                    mutation();
                    return true;
                });
            var firstModel = ModelWith("helm_004");
            var secondModel = ModelWith("helm_005");
            var thirdModel = ModelWith("helm_053");
            firstModel.Meshes.Single().UsesItemTintOverrides = true;
            secondModel.Meshes.Single().UsesItemTintOverrides = true;
            thirdModel.Meshes.Single().UsesItemTintOverrides = true;
            var firstColor = Color.FromRgb(12, 34, 56);
            var secondColor = Color.FromRgb(65, 43, 21);

            editor.Reload(firstModel, includeNonItemOwnedMaterials: false);
            editor.Colors.Single(row => row.Layer == TintMapLayerType.Cloth1).Color = firstColor;
            editor.Reload(
                null,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true,
                coalesceOrigin: firstOrigin);
            editor.Reload(
                secondModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);

            editor.Colors.Single(row => row.Layer == TintMapLayerType.Cloth1).Color = secondColor;
            editor.Reload(
                null,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true,
                coalesceOrigin: secondOrigin);
            editor.Reload(
                thirdModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);

            carryAttempts.Should().Be(2);
            appliedOrigin.Should().BeSameAs(secondOrigin);
            var thirdKey = TintMapVariable.GetName("helm_053", TintMapLayerType.Cloth1);
            TintMapColor.TryFromStoredValue(variables.GetInt(thirdKey)!.Value, out var carried)
                .Should().BeTrue();
            carried.Should().Be(new TintMapColor(secondColor.R, secondColor.G, secondColor.B),
                "the rejected first snapshot must not override the newer model color");
        }

        [Test]
        public void ItemModelReplacementDoesNotGuessBetweenDifferentColorsForOneLayer()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var variables = new VarTable(new JsonGffStruct());
            var editor = new TintMapEditorViewModel(
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);
            var oldModel = new RenderModel
            {
                Meshes = new[]
                {
                    ModelWith("helm_004").Meshes.Single(),
                    ModelWith("helm_053").Meshes.Single()
                }
            };
            foreach (var mesh in oldModel.Meshes)
                mesh.UsesItemTintOverrides = true;
            var newModel = ModelWith("helm_005");
            newModel.Meshes.Single().UsesItemTintOverrides = true;
            variables.SetInt(
                TintMapVariable.GetName("helm_004", TintMapLayerType.Cloth1),
                new TintMapColor(12, 34, 56).ToStoredValue());
            variables.SetInt(
                TintMapVariable.GetName("helm_053", TintMapLayerType.Cloth1),
                new TintMapColor(65, 43, 21).ToStoredValue());

            editor.Reload(
                oldModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);
            editor.Reload(
                null,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);
            editor.Reload(
                newModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);

            variables.GetInt(TintMapVariable.GetName("helm_005", TintMapLayerType.Cloth1))
                .Should().BeNull("different per-material colors are ambiguous and must not be guessed");
            variables.GetInt(TintMapVariable.GetName("helm_004", TintMapLayerType.Cloth1))
                .Should().BeNull("ambiguous obsolete colors must not resurrect with the old model");
            variables.GetInt(TintMapVariable.GetName("helm_053", TintMapLayerType.Cloth1))
                .Should().BeNull("every obsolete per-material color must be cleaned up");
        }

        [Test]
        public void ItemModelReplacementPreservesPartialCustomColorByMaterialPosition()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var variables = new VarTable(new JsonGffStruct());
            var editor = new TintMapEditorViewModel(
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);
            var oldModel = new RenderModel
            {
                Meshes = new[]
                {
                    ModelWith("helm_004").Meshes.Single(),
                    ModelWith("helm_053").Meshes.Single()
                }
            };
            var newModel = new RenderModel
            {
                Meshes = new[]
                {
                    ModelWith("helm_013").Meshes.Single(),
                    ModelWith("helm_109").Meshes.Single()
                }
            };
            foreach (var mesh in oldModel.Meshes.Concat(newModel.Meshes))
                mesh.UsesItemTintOverrides = true;
            var oldCustomKey = TintMapVariable.GetName("helm_004", TintMapLayerType.Cloth1);
            var oldPresetKey = TintMapVariable.GetName("helm_053", TintMapLayerType.Cloth1);
            var newCustomKey = TintMapVariable.GetName("helm_013", TintMapLayerType.Cloth1);
            var newPresetKey = TintMapVariable.GetName("helm_109", TintMapLayerType.Cloth1);
            variables.SetInt(oldCustomKey, new TintMapColor(12, 34, 56).ToStoredValue());

            editor.Reload(
                oldModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);
            editor.Reload(
                null,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);
            editor.Reload(
                newModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);

            TintMapColor.TryFromStoredValue(variables.GetInt(newCustomKey)!.Value, out var carried)
                .Should().BeTrue();
            carried.Should().Be(new TintMapColor(12, 34, 56));
            variables.GetInt(newPresetKey).Should().BeNull(
                "a preset source position must leave the corresponding replacement on its preset");
            variables.GetInt(oldCustomKey).Should().BeNull();
            variables.GetInt(oldPresetKey).Should().BeNull();
        }

        [Test]
        public void ItemModelReplacementMatchesWearerVariantMaterialSlotsBeforeListPosition()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            catalog!.AreEquipmentMaterialSlotsEquivalent(
                    "pfh0_pelvis268",
                    "pmh0_pelvis268",
                    TintMapLayerType.Leather2)
                .Should().BeTrue();

            var variables = new VarTable(new JsonGffStruct());
            var editor = new TintMapEditorViewModel(
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog);
            var femaleModel = new RenderModel
            {
                Meshes = new[]
                {
                    ModelWith("pfh0_p_pe_714899").Meshes.Single(),
                    ModelWith("pfh0_pelvis268").Meshes.Single()
                }
            };
            var maleModel = ModelWith("pmh0_pelvis268");
            foreach (var mesh in femaleModel.Meshes.Concat(maleModel.Meshes))
                mesh.UsesItemTintOverrides = true;
            var color = new TintMapColor(12, 34, 56);
            variables.SetInt(
                TintMapVariable.GetName("pfh0_pelvis268", TintMapLayerType.Leather2),
                color.ToStoredValue());

            editor.Reload(
                femaleModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);
            editor.Reload(
                null,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);
            editor.Reload(
                maleModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);

            var stored = variables.GetInt(
                TintMapVariable.GetName("pmh0_pelvis268", TintMapLayerType.Leather2));
            stored.Should().NotBeNull();
            TintMapColor.TryFromStoredValue(stored!.Value, out var carried).Should().BeTrue();
            carried.Should().Be(color);
            variables.GetInt(
                    TintMapVariable.GetName("pfh0_p_pe_714899", TintMapLayerType.Leather2))
                .Should().BeNull("the first female list position was never the custom source");
        }

        [Test]
        public void ItemModelReplacementCarriesLoneLaterSlotTintToLoneDestination()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var variables = new VarTable(new JsonGffStruct());
            var editor = new TintMapEditorViewModel(
                variables,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);
            var oldModel = new RenderModel
            {
                Meshes = new[]
                {
                    ModelWith("pfh0_p_pe_714899").Meshes.Single(),
                    ModelWith("pfh0_pelvis268").Meshes.Single()
                }
            };
            var newModel = ModelWith("pfh0_pelvis269");
            foreach (var mesh in oldModel.Meshes.Concat(newModel.Meshes))
                mesh.UsesItemTintOverrides = true;
            var layer = TintMapLayerType.Cloth2;
            var oldKey = TintMapVariable.GetName("pfh0_pelvis268", layer);
            var newKey = TintMapVariable.GetName("pfh0_pelvis269", layer);
            var color = new TintMapColor(12, 34, 56);
            variables.SetInt(oldKey, color.ToStoredValue());

            editor.Reload(
                oldModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);
            editor.Reload(
                null,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);
            editor.Reload(
                newModel,
                includeNonItemOwnedMaterials: false,
                carryItemCustomColorsAcrossMaterials: true);

            var stored = variables.GetInt(newKey);
            stored.Should().NotBeNull();
            TintMapColor.TryFromStoredValue(stored!.Value, out var carried).Should().BeTrue();
            carried.Should().Be(color);
            variables.GetInt(oldKey).Should().BeNull();
        }

        [Test]
        public void CatalogReloadClearsStaleRowsBeforeTheNewModelIsResolved()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var editor = new TintMapEditorViewModel(
                new VarTable(new JsonGffStruct()),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                catalog!);
            editor.Reload(ModelWith("pmo0_footl10"));
            editor.Colors.Should().NotBeEmpty();

            editor.ReloadCatalog(null);

            editor.Colors.Should().BeEmpty();
            editor.HasColors.Should().BeFalse();
        }

        [Test]
        public void ExistingEditorsCreateTintControlsWhenCatalogAppears()
        {
            var catalog = TintMapCatalog.Load(Resources());
            catalog.Should().NotBeNull();
            var creatureRoot = new ModuleWorkspace(CorpusLocator.ModuleDirectory)
                .LoadBlueprint(ResourceType.Utc, "agr_guildmaster")
                .Document.Root;
            var itemRoot = new ModuleWorkspace(CorpusLocator.ModuleDirectory)
                .LoadBlueprint(ResourceType.Uti, "adren_harness")
                .Document.Root;
            static bool Edit(string _, Action mutation)
            {
                mutation();
                return true;
            }

            using var creatureEditor = new CreatureEditorViewModel(
                creatureRoot,
                Path.Combine(
                    CorpusLocator.ModuleDirectory,
                    "utc",
                    "agr_guildmaster.utc.json"),
                "agr_guildmaster",
                Edit,
                null,
                null,
                null,
                null,
                _ => null,
                null);
            using var itemEditor = new ItemEditorViewModel(
                itemRoot,
                "adren_harness",
                Edit,
                baseItemRows: baseItem => baseItem == 16
                    ? new BaseItemRow(16, "armor", 3)
                    : null,
                baseItemIcons: baseItem => baseItem == 16
                    ? new BaseItemIconRow(16, 3, "AArCl", "gifp")
                    : null,
                textureExists: _ => true);

            creatureEditor.TintMapEditor.Should().BeNull();
            itemEditor.TintMapEditor.Should().BeNull();

            creatureEditor.ReloadTintMapCatalog(catalog);
            itemEditor.ReloadTintMapCatalog(catalog);

            creatureEditor.TintMapEditor.Should().NotBeNull();
            creatureEditor.HasTintMapEditor.Should().BeTrue();
            itemEditor.TintMapEditor.Should().NotBeNull();
            itemEditor.Appearance!.Tints.Should().BeSameAs(itemEditor.TintMapEditor);

            creatureEditor.ReloadTintMapCatalog(null);
            itemEditor.ReloadTintMapCatalog(null);

            creatureEditor.TintMapEditor.Should().BeNull();
            creatureEditor.HasTintMapEditor.Should().BeFalse();
            itemEditor.TintMapEditor.Should().BeNull();
            itemEditor.Appearance!.Tints.Should().BeNull();
        }

        [AvaloniaTest]
        public void TintEditorViewLoadsTheAvaloniaColorPicker()
        {
            var view = new TintMapEditorView
            {
                DataContext = new TintMapEditorViewModel(
                    new VarTable(new JsonGffStruct()),
                    (_, mutation) =>
                    {
                        mutation();
                        return true;
                    },
                    TintMapCatalog.Load(Resources())!)
            };
            ((TintMapEditorViewModel)view.DataContext!).Reload(ModelWith("pmo0_footl10"));
            var window = new Window { Content = view };

            window.Show();
            try
            {
                view.GetVisualDescendants().OfType<ColorPicker>().Should().NotBeEmpty();
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaTest]
        public void ApplicationTemplateRendersTintEditorInsideAContentControl()
        {
            var editor = new TintMapEditorViewModel(
                new VarTable(new JsonGffStruct()),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                TintMapCatalog.Load(Resources())!);
            editor.Reload(ModelWith("pmh0_robe010"));
            var host = new ContentControl { Content = editor };
            var window = new Window { Content = host };

            window.Show();
            try
            {
                Dispatcher.UIThread.RunJobs();
                host.GetVisualDescendants().OfType<TintMapEditorView>().Should().ContainSingle(
                    "the shared application template must replace the view model object with its view");
                host.GetVisualDescendants().OfType<ColorPicker>().Should().NotBeEmpty(
                    "item editors must expose unrestricted custom RGB controls");
                host.GetVisualDescendants().OfType<TextBlock>()
                    .Select(text => text.Text)
                    .Should()
                    .NotContain(typeof(TintMapEditorViewModel).FullName);
            }
            finally
            {
                window.Close();
            }
        }

        private static RenderModel ItemOwnedModelWith(string material)
        {
            var model = ModelWith(material);
            model.Meshes.Single().UsesItemTintOverrides = true;
            return model;
        }

        private static void DrainUntil(Func<bool> condition)
        {
            for (var attempt = 0; attempt < 200 && !condition(); attempt++)
            {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(5);
            }

            Dispatcher.UIThread.RunJobs();
            condition().Should().BeTrue("the background preview should publish promptly");
        }

        private sealed class NoOpDocumentEdit : IDocumentEdit
        {
            private readonly string _description;

            public NoOpDocumentEdit(string description)
            {
                _description = description;
            }

            public void Apply()
            {
            }

            public void Revert()
            {
            }

            public string Describe() => _description;
        }
    }
}
