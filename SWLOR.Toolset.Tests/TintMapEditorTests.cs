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
            TintMapColor.TryFromStoredValue(store.Locals.GetInt(key)!.Value, out var custom)
                .Should().BeTrue();
            custom.Should().Be(new TintMapColor(12, 34, 56));
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
        public void MannequinSexChangeDoesNotMoveOrDeleteGenderSpecificCustomColors()
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

            variables.GetInt(maleKey).Should().NotBeNull(
                "changing only the preview mannequin must not delete the male material's tint");
            variables.GetInt(femaleKey).Should().BeNull(
                "changing only the preview mannequin must not copy a male tint onto female materials");
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
            currentAppliedEdit = unrelatedOrigin;
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
