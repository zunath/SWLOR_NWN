using Nwn.Authoring.Areas.Editing;
using Nwn.Authoring.Editing;
using Nwn.Toolset.Avalonia.Areas;
using Nwn.Preview.Areas.Clipboard;
using Nwn.Authoring.Areas.Placement;
using System.Numerics;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using Nwn.Authoring.Documents.Native;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using Nwn.Authoring.Documents.NimGff;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>Object placement updates one marker instead of rebuilding a complete area scene.</summary>
    [TestFixture]
    public sealed class AreaPlacementIncrementalTests
    {
        private const string AreaResRef = "prefab_space";
        private const string PlaceableResRef = "rugpitpcbl";
        private string _moduleRoot = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _moduleRoot = Path.Combine(
                Path.GetTempPath(), $"swlor-placement-fast-{Guid.NewGuid():N}");
            foreach (var folder in CorpusLocator.GffFolders)
                Directory.CreateDirectory(Path.Combine(_moduleRoot, folder));

            foreach (var folder in new[] { "are", "git", "gic" })
            {
                File.Copy(
                    Path.Combine(CorpusLocator.ModuleDirectory, folder, $"{AreaResRef}.{folder}.json"),
                    Path.Combine(_moduleRoot, folder, $"{AreaResRef}.{folder}.json"));
            }

            File.Copy(
                Path.Combine(CorpusLocator.ModuleDirectory, "utp", $"{PlaceableResRef}.utp.json"),
                Path.Combine(_moduleRoot, "utp", $"{PlaceableResRef}.utp.json"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_moduleRoot))
                Directory.Delete(_moduleRoot, recursive: true);
        }

        [AvaloniaTest]
        public async Task PlacementPublishesOneMarkerWithoutASecondFullSceneBuild()
        {
            var editor = await CreateEditorAsync();
            editor.EnsureSceneBuilt();
            await WaitUntilAsync(() => editor.AreaScene != null && !editor.IsBuildingScene);

            var before = editor.AreaScene!;
            var originalInstanceCount = before.Instances.Count;
            var originalPlaceableCount = before.Instances.Count(marker => marker.Kind == Nwn.Preview.Areas.InstanceMarkerKind.Placeable);
            editor.ArmPlacement(
                ResourceType.Utp,
                PlaceableResRef,
                PaletteSource.Custom).Should().BeTrue();

            editor.CommitPlacement(new Vector3(12.5f, 7.25f, 0f));

            var published = editor.AreaScene;
            published.Should().NotBeSameAs(before, "the placed marker is published synchronously");
            published!.Tiles.Should().BeSameAs(before.Tiles,
                "placement must not rebuild or copy the area's tile grid");
            published.Diagnostics.Should().BeSameAs(before.Diagnostics);
            published.Instances.Should().HaveCount(originalInstanceCount + 1);
            editor.SelectedSceneInstance.Should().BeSameAs(published.Instances[^1]);
            editor.SelectedSceneInstance!.TemplateResRef.Should().Be(PlaceableResRef);
            editor.SelectedSceneInstance.Position.Should().Be(new Vector3(12.5f, 7.25f, 0f));
            editor.SelectedSceneInstance.ListIndex.Should().Be(originalPlaceableCount);
            var placeableSection = editor.Sections.Single(section => section.BlueprintType == ResourceType.Utp);
            placeableSection.Rows[originalPlaceableCount].TemplateResRef.Should().Be(PlaceableResRef);
            placeableSection.SelectedRow.Should().BeSameAs(placeableSection.Rows[originalPlaceableCount]);
            editor.IsBuildingScene.Should().BeFalse();

            // Run past the ordinary edit debounce. If the placement failed to claim the new scene
            // revision, that delayed refresh would still replace this cheap scene with a full build.
            await Task.Delay(350);
            editor.AreaScene.Should().BeSameAs(published);
            editor.IsBuildingScene.Should().BeFalse();
        }

        [AvaloniaTest]
        public async Task CopyPasteArmsTheCursorAndPreservesTheCompleteInstanceAndComment()
        {
            AddAuthoredSourcePlacement();
            var editor = await CreateEditorAsync(new AreaInstanceClipboard());
            editor.EnsureSceneBuilt();
            await WaitUntilAsync(() => editor.AreaScene != null && !editor.IsBuildingScene);

            var source = editor.AreaScene!.Instances.Should().ContainSingle(
                marker => marker.Kind == InstanceMarkerKind.Placeable).Which;
            AssertFacingNorth(source.Orientation);
            editor.SelectSceneInstance(source);

            editor.CopySelectedSceneInstance().Should().BeTrue();

            // Prove Ctrl+C took an independent snapshot rather than remembering the live row.
            editor.RotateSelectedInstance(source, new Vector2(1f, 0f));
            editor.PasteCopiedSceneInstance().Should().BeTrue();
            editor.IsPlacementPending.Should().BeTrue();
            editor.PlacementGhost.Should().NotBeNull();
            editor.PlacementGhost!.TemplateResRef.Should().Be(PlaceableResRef);
            AssertFacingNorth(editor.PlacementGhost.Orientation);

            var destination = new Vector3(18.5f, 21.25f, 0f);
            editor.CommitPlacement(destination);

            editor.IsPlacementPending.Should().BeFalse();
            editor.AreaScene!.Instances.Should().HaveCount(2);
            editor.SelectedSceneInstance.Should().NotBeNull();
            var pastedMarker = editor.SelectedSceneInstance!;
            pastedMarker.Position.Should().Be(destination);
            AssertFacingNorth(pastedMarker.Orientation);

            (await editor.TrySaveAsync()).Should().BeTrue();
            var savedGit = GitDocument.Load(Path.Combine(
                _moduleRoot, "git", $"{AreaResRef}.git.json"));
            savedGit.Placeables.Should().HaveCount(2);
            var pasted = savedGit.Placeables[1];
            pasted.GetStringOrNull("COPY_SENTINEL").Should().Be("preserve the full instance");
            pasted.GetStringOrNull("Tag").Should().Be("copied_source_tag");
            InstanceFieldMap.GetPosition(ResourceType.Utp, pasted)
                .Should().Be((destination.X, destination.Y, destination.Z));
            var pastedFacing = InstanceFieldMap.GetOrientation(ResourceType.Utp, pasted);
            pastedFacing.XOrientation.Should().BeApproximately(0f, 0.00001f);
            pastedFacing.YOrientation.Should().BeApproximately(1f, 0.00001f,
                "paste retains the copied heading unless a doorway supplies one");

            var savedGic = GicDocument.Load(Path.Combine(
                _moduleRoot, "gic", $"{AreaResRef}.gic.json"));
            savedGic.Placeables.Should().HaveCount(2);
            GicDocument.GetComment(savedGic.Placeables[1]).Should().Be("builder note survives copy");

            editor.UndoInstancesCommand.Execute(null);
            editor.Sections.Single(section => section.BlueprintType == ResourceType.Utp)
                .Rows.Should().ContainSingle("paste is one undoable edit");
            (await editor.TrySaveAsync()).Should().BeTrue();
            GicDocument.Load(Path.Combine(_moduleRoot, "gic", $"{AreaResRef}.gic.json"))
                .Placeables.Should().ContainSingle("undo removes the paired copied comment too");
        }

        [AvaloniaTest]
        public async Task AreaEditorViewRoutesControlCopyAndPasteToPlacement()
        {
            AddAuthoredSourcePlacement();
            var editor = await CreateEditorAsync(new AreaInstanceClipboard());
            editor.EnsureSceneBuilt();
            await WaitUntilAsync(() => editor.AreaScene != null && !editor.IsBuildingScene);
            editor.SelectSceneInstance(editor.AreaScene!.Instances.Should().ContainSingle(
                marker => marker.Kind == InstanceMarkerKind.Placeable).Which);

            var view = new ShortcutAreaEditorView { DataContext = editor };

            view.SendControlKey(Key.C).Should().BeTrue();
            view.SendControlKey(Key.V).Should().BeTrue();
            editor.IsPlacementPending.Should().BeTrue();
            editor.PlacementGhost.Should().NotBeNull();
            editor.PlacementGhost!.TemplateResRef.Should().Be(PlaceableResRef);
        }

        [AvaloniaTest]
        public async Task CutKeepsAnIndependentCopyAndUndoRestoresItsComment()
        {
            AddAuthoredSourcePlacement();
            var editor = await CreateEditorAsync();
            editor.EnsureSceneBuilt();
            await WaitUntilAsync(() => editor.AreaScene != null && !editor.IsBuildingScene);
            editor.SelectSceneInstance(editor.AreaScene!.Instances.Single());
            var view = new ShortcutAreaEditorView { DataContext = editor };

            view.SendControlKey(Key.X).Should().BeTrue();
            var section = editor.Sections.Single(section => section.BlueprintType == ResourceType.Utp);
            section.Rows.Should().BeEmpty();
            editor.UndoInstancesCommand.Execute(null);
            section.Rows.Should().ContainSingle();
            editor.PasteCopiedSceneInstance().Should().BeTrue();
            editor.CommitPlacement(new Vector3(8, 9, 0));
            section.Rows.Should().HaveCount(2);
            (await editor.TrySaveAsync()).Should().BeTrue();
            var saved = GicDocument.Load(Path.Combine(_moduleRoot, "gic", $"{AreaResRef}.gic.json"));
            saved.Placeables.Should().HaveCount(2);
            GicDocument.GetComment(saved.Placeables[1]).Should().Be("builder note survives copy");
        }

        [AvaloniaTest]
        public async Task TextFieldsKeepTheirOwnClipboardAndSpaceKeys()
        {
            AddAuthoredSourcePlacement();
            var editor = await CreateEditorAsync();
            editor.EnsureSceneBuilt();
            await WaitUntilAsync(() => editor.AreaScene != null && !editor.IsBuildingScene);
            editor.SelectSceneInstance(editor.AreaScene!.Instances.Single());
            var view = new ShortcutAreaEditorView { DataContext = editor };
            var text = new Avalonia.Controls.TextBox { Text = "Builder text" };
            view.SendKey(Key.X, KeyModifiers.Control, text).Should().BeFalse();
            view.SendKey(Key.C, KeyModifiers.Control, text).Should().BeFalse();
            view.SendKey(Key.V, KeyModifiers.Control, text).Should().BeFalse();
            view.SendKey(Key.Space, source: text).Should().BeFalse();
            editor.Sections.Single(section => section.BlueprintType == ResourceType.Utp)
                .Rows.Should().ContainSingle();
            editor.IsPlacementPending.Should().BeFalse();
        }

        [AvaloniaTest]
        public async Task RepeatedPlacementKeepsTheBrushUntilAnOrdinaryPlacementOrEscape()
        {
            var editor = await CreateEditorAsync();
            editor.EnsureSceneBuilt();
            await WaitUntilAsync(() => editor.AreaScene != null && !editor.IsBuildingScene);
            editor.ArmPlacement(ResourceType.Utp, PlaceableResRef, PaletteSource.Custom).Should().BeTrue();
            editor.CommitPlacement(new Vector3(2, 3, 0), keepBrush: true);
            editor.IsPlacementPending.Should().BeTrue();
            editor.CommitPlacement(new Vector3(4, 5, 0), keepBrush: true);
            editor.AreaScene!.Instances.Should().HaveCount(2);
            editor.CommitPlacement(new Vector3(6, 7, 0));
            editor.IsPlacementPending.Should().BeFalse();
            editor.AreaScene!.Instances.Should().HaveCount(3);

            editor.ArmPlacement(ResourceType.Utp, PlaceableResRef, PaletteSource.Custom).Should().BeTrue();
            var view = new ShortcutAreaEditorView { DataContext = editor };
            view.SendKey(Key.Escape).Should().BeTrue();
            editor.IsPlacementPending.Should().BeFalse();
            editor.SelectedSceneInstance.Should().BeNull();
        }

        [AvaloniaTest]
        public async Task SpaceDropsToTheWalkmeshAndUndoRestoresAuthoredHeight()
        {
            AddAuthoredSourcePlacement();
            var editor = await CreateEditorAsync();
            editor.EnsureSceneBuilt();
            await WaitUntilAsync(() => editor.AreaScene != null && !editor.IsBuildingScene);
            var source = editor.AreaScene!.Instances.Single();
            editor.MoveSelectedInstance(source, new Vector3(4, 6, 8));
            var scene = editor.AreaScene!;
            var groundedScene = new AreaScene
            {
                Tileset = scene.Tileset, Width = scene.Width, Height = scene.Height,
                Instances = scene.Instances, Diagnostics = scene.Diagnostics,
                Tiles = new[]
                {
                    new TilePlacement
                    {
                        TileIndex = 0, Column = 0, Row = 0, TileId = 0, Orientation = 0,
                        HeightLevel = 0, HeightOffset = 0, CenterX = 5, CenterY = 5,
                        Transform = Matrix4x4.Identity, IsFallback = false,
                        Walkmesh = new WalkMesh
                        {
                            Vertices = new[] { new Vector3(0, 0, 2), new Vector3(10, 0, 2),
                                new Vector3(10, 10, 2), new Vector3(0, 10, 2) },
                            Faces = new[] { new WalkFace { A = 0, B = 1, C = 2, Material = 1, Walkable = true },
                                new WalkFace { A = 0, B = 2, C = 3, Material = 1, Walkable = true } }
                        }
                    }
                }
            };
            typeof(AreaEditorViewModel).GetProperty(nameof(AreaEditorViewModel.AreaScene))!
                .SetValue(editor, groundedScene);
            var view = new ShortcutAreaEditorView { DataContext = editor };
            view.SendKey(Key.Space).Should().BeTrue();
            editor.SelectedSceneInstance!.Position.Should().Be(new Vector3(4, 6, 2));
            editor.UndoInstancesCommand.Execute(null);
            (await editor.TrySaveAsync()).Should().BeTrue();
            var saved = GitDocument.Load(Path.Combine(_moduleRoot, "git", $"{AreaResRef}.git.json"));
            InstanceFieldMap.GetPosition(ResourceType.Utp, saved.Placeables[0]).Z.Should().Be(8);
        }

        [AvaloniaTest]
        public async Task GroupClipboardPreservesOffsetsAndCutsAndPastesAsSingleUndoSteps()
        {
            AddAuthoredSourcePlacement();
            var editor = await CreateEditorAsync();
            editor.EnsureSceneBuilt();
            await WaitUntilAsync(() => editor.AreaScene != null && !editor.IsBuildingScene);
            var original = editor.AreaScene!.Instances.Single();
            editor.SelectSceneInstance(original);
            editor.CopySelectedSceneInstance().Should().BeTrue();
            editor.PasteCopiedSceneInstance().Should().BeTrue();
            editor.CommitPlacement(new Vector3(14, 16, 3));
            editor.SelectSceneSelection(new AreaSelectionPick(editor.AreaScene!.Instances, Array.Empty<(int, int)>(), false));
            editor.SelectedSceneInstances.Should().HaveCount(2);
            editor.CutSelectedSceneInstance().Should().BeTrue();
            var section = editor.Sections.Single(section => section.BlueprintType == ResourceType.Utp);
            section.Rows.Should().BeEmpty();
            editor.UndoInstancesCommand.Execute(null);
            section.Rows.Should().HaveCount(2);
            editor.PasteCopiedSceneInstance().Should().BeTrue();
            editor.PlacementGhosts.Should().HaveCount(2);
            editor.CommitPlacement(new Vector3(24, 26, 0));
            section.Rows.Should().HaveCount(4);
            (await editor.TrySaveAsync()).Should().BeTrue();
            var saved = GitDocument.Load(Path.Combine(_moduleRoot, "git", $"{AreaResRef}.git.json"));
            InstanceFieldMap.GetPosition(ResourceType.Utp, saved.Placeables[2]).Should().Be((24f, 26f, 0f));
            InstanceFieldMap.GetPosition(ResourceType.Utp, saved.Placeables[3]).Should().Be((34f, 36f, 3f));
            var comments = GicDocument.Load(Path.Combine(_moduleRoot, "gic", $"{AreaResRef}.gic.json"));
            comments.Placeables.Should().HaveCount(4);
            GicDocument.GetComment(comments.Placeables[3]).Should().Be("builder note survives copy");
            editor.UndoInstancesCommand.Execute(null);
            section.Rows.Should().HaveCount(2);
        }

        [AvaloniaTest]
        public async Task TileClipboardPreservesOrientationHeightAndUndoThroughTheAreaView()
        {
            var path = Path.Combine(_moduleRoot, "are", $"{AreaResRef}.are.json");
            var document = JsonGffDocument.Load(path);
            var tiles = document.Root.Get("Tile_List").Elements!;
            tiles[0].SetInt("Tile_Orientation", GffFieldType.Int, 3);
            tiles[1].SetInt("Tile_Height", GffFieldType.Int, 2);
            tiles[0].SetString("Sentinel", GffFieldType.CExoString, "authored tile data");
            File.WriteAllBytes(path, document.ToBytes());
            var editor = await CreateEditorAsync();
            editor.SelectSceneSelection(new AreaSelectionPick(Array.Empty<InstanceMarker>(), new[] { (0, 0), (1, 0) }, false));
            var view = new ShortcutAreaEditorView { DataContext = editor };
            view.SendControlKey(Key.C).Should().BeTrue();
            view.SendControlKey(Key.V).Should().BeTrue();
            editor.CommitTilePlacement(2, 0);
            (await editor.TrySaveAsync()).Should().BeTrue();
            var saved = JsonGffDocument.Load(path).Root.Get("Tile_List").Elements!;
            saved[2].GetIntOrNull("Tile_Orientation").Should().Be(3);
            saved[3].GetIntOrNull("Tile_Height").Should().Be(2);
            saved[2].GetStringOrNull("Sentinel").Should().Be("authored tile data");
            editor.UndoAreCommand.Execute(null);
            (await editor.TrySaveAsync()).Should().BeTrue();
            var undone = JsonGffDocument.Load(path).Root.Get("Tile_List").Elements!;
            undone[2].GetStringOrNull("Sentinel").Should().BeNull();
            undone[3].GetIntOrNull("Tile_Height").Should().Be(0);
        }

        [AvaloniaTest]
        public async Task PlayerStartRotationSavesModuleDirectionAndUndoesAfterSave()
        {
            var path = Path.Combine(_moduleRoot, "ifo", "module.ifo.json");
            var sourcePath = Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var document = JsonGffDocument.Load(sourcePath);
            document.Root.SetString("Mod_Entry_Area", GffFieldType.ResRef, AreaResRef);
            document.Root.SetSingle("Mod_Entry_Dir_X", 1);
            document.Root.SetSingle("Mod_Entry_Dir_Y", 0);
            File.WriteAllBytes(path, document.ToBytes());
            using var session = DocumentSession.Open(path);
            var entry = new ModuleEntryEditor(session);
            var editor = await CreateEditorAsync(moduleEntry: entry);
            editor.EnsureSceneBuilt();
            await WaitUntilAsync(() => editor.AreaScene != null && !editor.IsBuildingScene);
            var start = editor.AreaScene!.Instances.Single(marker => marker.Kind == InstanceMarkerKind.StartLocation);
            editor.SelectSceneInstance(start);
            var properties = new SWLOR.Toolset.Editors.Module.ModulePropertiesDocumentViewModel(
                path, _moduleRoot, new ModuleWorkspace(_moduleRoot), new OutputLogService(),
                new StubPrompts(), moduleEntry: entry);
            properties.Tag = "shared_module_tag";
            editor.RotateSelectedInstance(start, Vector2.UnitY);
            properties.Document.EntryDirectionY.Should().Be(1);
            entry.Document.EntryDirectionY.Should().Be(1);
            editor.IsDirty.Should().BeTrue();
            (await editor.TrySaveAsync()).Should().BeTrue();
            IfoDocument.Load(path).EntryDirectionY.Should().Be(1);
            IfoDocument.Load(path).Tag.Should().Be("shared_module_tag");
            properties.IsDirty.Should().BeFalse();
            properties.OnClose().Should().BeTrue("closing Module Properties must keep the borrowed session alive");
            editor.UndoAreCommand.Execute(null);
            entry.Document.EntryDirectionX.Should().Be(1);
            (await editor.TrySaveAsync()).Should().BeTrue();
            IfoDocument.Load(path).EntryDirectionX.Should().Be(1);
            IfoDocument.Load(path).EntryDirectionY.Should().Be(0);
        }

        [AvaloniaTest]
        public async Task LockedModuleEntryRollsBackItsCompanionAreaSave()
        {
            Support.WindowsFileSharing.Require();
            var path = Path.Combine(_moduleRoot, "ifo", "module.ifo.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var document = JsonGffDocument.Load(Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json"));
            document.Root.SetString("Mod_Entry_Area", GffFieldType.ResRef, AreaResRef);
            File.WriteAllBytes(path, document.ToBytes());
            using var session = DocumentSession.Open(path);
            var entry = new ModuleEntryEditor(session);
            var editor = await CreateEditorAsync(moduleEntry: entry);
            editor.EnsureSceneBuilt();
            await WaitUntilAsync(() => editor.AreaScene != null && !editor.IsBuildingScene);
            editor.RotateSelectedInstance(editor.AreaScene!.Instances.Single(marker =>
                marker.Kind == InstanceMarkerKind.StartLocation), new Vector2(0.6f, 0.8f));
            editor.SelectTile((0, 0));
            editor.RaiseTileCommand.Execute(null);
            var areaPath = Path.Combine(_moduleRoot, "are", $"{AreaResRef}.are.json");
            var originalArea = File.ReadAllBytes(areaPath);
            var originalModule = File.ReadAllBytes(path);
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                (await editor.TrySaveAsync()).Should().BeFalse();
            }
            File.ReadAllBytes(areaPath).Should().Equal(originalArea);
            File.ReadAllBytes(path).Should().Equal(originalModule);
            editor.IsDirty.Should().BeTrue();
            entry.IsEntryDirty.Should().BeTrue();
            (await editor.TrySaveAsync()).Should().BeTrue();
        }

        [AvaloniaTest]
        public async Task MixedPlayerStartSelectionCopiesAndDeletesOnlyPlacedObjects()
        {
            AddAuthoredSourcePlacement();
            var path = Path.Combine(_moduleRoot, "ifo", "module.ifo.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var document = JsonGffDocument.Load(Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json"));
            document.Root.SetString("Mod_Entry_Area", GffFieldType.ResRef, AreaResRef);
            File.WriteAllBytes(path, document.ToBytes());
            using var session = DocumentSession.Open(path);
            var entry = new ModuleEntryEditor(session);
            var clipboard = new AreaInstanceClipboard();
            var editor = await CreateEditorAsync(clipboard, entry);
            editor.EnsureSceneBuilt();
            await WaitUntilAsync(() => editor.AreaScene != null && !editor.IsBuildingScene);
            editor.SelectSceneSelection(new AreaSelectionPick(editor.AreaScene!.Instances, Array.Empty<(int, int)>(), false));
            editor.SelectedSceneInstances.Should().HaveCount(2);
            var view = new ShortcutAreaEditorView { DataContext = editor };
            view.SendControlKey(Key.C).Should().BeTrue();
            clipboard.Contents.Should().ContainSingle().Which.Type.Should().Be(ResourceType.Utp);
            view.SendKey(Key.Delete).Should().BeTrue();
            editor.Sections.Single(section => section.BlueprintType == ResourceType.Utp).Rows.Should().BeEmpty();
            entry.IsDirty.Should().BeFalse();
            editor.UndoInstancesCommand.Execute(null);
            editor.Sections.Single(section => section.BlueprintType == ResourceType.Utp).Rows.Should().ContainSingle();
        }

        [AvaloniaTest]
        public void ReopeningModulePropertiesRefreshesAnUnownedCleanCachedIfo()
        {
            var path = Path.Combine(_moduleRoot, "ifo", "module.ifo.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.Copy(Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json"), path);
            var log = new OutputLogService();
            var context = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            context.OpenAndSettle(_moduleRoot);
            var factory = new SWLOR.Toolset.Shell.ToolsetDockFactory(null!, null!, null!, null!, null!, null!, null!, null!, null!);
            using var service = new EditorService(context, new LookupOptionProvider(context), log, factory, new StubPrompts());
            SWLOR.Toolset.Editors.Module.ModulePropertiesDocumentViewModel Current() =>
                (SWLOR.Toolset.Editors.Module.ModulePropertiesDocumentViewModel)typeof(EditorService)
                    .GetField("_moduleProperties", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                    .GetValue(service)!;
            service.OpenModuleProperties();
            Current().OnClose().Should().BeTrue();
            var external = JsonGffDocument.Load(path);
            external.Root.SetString("Mod_Tag", GffFieldType.CExoString, "external_module_tag");
            File.WriteAllBytes(path, external.ToBytes());
            service.OpenModuleProperties();
            Current().Tag.Should().Be("external_module_tag");
            Current().IsDirty.Should().BeFalse();
            Current().OnClose().Should().BeTrue();
            context.WaitForBackgroundScans();
        }

        [AvaloniaTest]
        public void UnreadableModuleEntryKeepsAreaEditingAvailableAndPreservesCachedData()
        {
            Support.WindowsFileSharing.Require();
            var path = Path.Combine(_moduleRoot, "ifo", "module.ifo.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var original = File.ReadAllBytes(Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json"));
            foreach (var cached in new[] { false, true })
            {
                File.WriteAllBytes(path, original);
                var log = new OutputLogService();
                var context = new WorkspaceContext(root => new ModuleWorkspace(root), log);
                context.OpenAndSettle(_moduleRoot);
                var factory = new SWLOR.Toolset.Shell.ToolsetDockFactory(null!, null!, null!, null!, null!, null!, null!, null!, null!);
                using var service = new EditorService(context, new LookupOptionProvider(context), log, factory, new StubPrompts());
                ModuleEntryEditor? OpenEntry() => (ModuleEntryEditor?)typeof(EditorService)
                    .GetMethod("ModuleEntryForWorkspace", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                    .Invoke(service, null);
                var expected = cached ? OpenEntry() : null;
                var expectedBytes = expected?.Session.ToBytes();
                using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                    OpenEntry().Should().BeSameAs(expected);
                File.WriteAllText(path, "malformed module data");
                OpenEntry().Should().BeSameAs(expected);
                if (expected != null) expected.Session.ToBytes().Should().Equal(expectedBytes!);
                log.Lines.Count(line => line.Contains("Could not open module entry data")).Should().Be(2);
                File.WriteAllBytes(path, original);
                OpenEntry().Should().NotBeNull();
                context.WaitForBackgroundScans();
            }
        }

        [AvaloniaTest]
        public async Task ModulePropertiesPublishesLocalChangesOnceAndRevertRefreshesArea()
        {
            var path = Path.Combine(_moduleRoot, "ifo", "module.ifo.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var document = JsonGffDocument.Load(Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json"));
            document.Root.SetString("Mod_Entry_Area", GffFieldType.ResRef, AreaResRef);
            document.Root.SetSingle("Mod_Entry_Dir_X", 1);
            document.Root.SetSingle("Mod_Entry_Dir_Y", 0);
            File.WriteAllBytes(path, document.ToBytes());
            using var session = DocumentSession.Open(path);
            var entry = new ModuleEntryEditor(session);
            var editor = await CreateEditorAsync(moduleEntry: entry);
            editor.EnsureSceneBuilt();
            await WaitUntilAsync(() => editor.AreaScene != null && !editor.IsBuildingScene);
            var properties = new SWLOR.Toolset.Editors.Module.ModulePropertiesDocumentViewModel(
                path, _moduleRoot, new ModuleWorkspace(_moduleRoot), new OutputLogService(),
                new StubPrompts(), moduleEntry: entry);
            var tagUpdates = 0;
            properties.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(properties.Tag)) tagUpdates++;
            };
            properties.Tag = "shared_module_tag";
            tagUpdates.Should().Be(1);
            properties.Undo();
            tagUpdates.Should().Be(2);
            properties.Redo();
            tagUpdates.Should().Be(3);
            editor.RotateSelectedInstance(editor.AreaScene!.Instances.Single(marker =>
                marker.Kind == InstanceMarkerKind.StartLocation), Vector2.UnitY);
            editor.IsDirty.Should().BeTrue();
            var sceneUpdates = 0;
            editor.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(editor.AreaScene)) sceneUpdates++;
            };
            properties.RevertCommand.Execute(null);
            sceneUpdates.Should().Be(1);
            properties.Tag.Should().NotBe("shared_module_tag");
            editor.AreaScene!.Instances.Single(marker => marker.Kind == InstanceMarkerKind.StartLocation)
                .Orientation.Should().Be(Vector2.UnitY);
            editor.IsDirty.Should().BeTrue("the area's separate rotation remains undoable");
            editor.UndoAreCommand.Execute(null);
            editor.IsDirty.Should().BeFalse();
            properties.IsDirty.Should().BeFalse();
            properties.OnClose().Should().BeTrue();
        }

        private void AddAuthoredSourcePlacement()
        {
            var gitPath = Path.Combine(_moduleRoot, "git", $"{AreaResRef}.git.json");
            var git = JsonGffDocument.Load(gitPath);
            var placeables = git.Root.GetOrNull("Placeable List");
            if (placeables == null)
            {
                placeables = JsonGffField.CreateList();
                git.Root.Add("Placeable List", placeables);
            }

            var blueprint = JsonGffDocument.Load(Path.Combine(
                _moduleRoot, "utp", $"{PlaceableResRef}.utp.json"));
            var source = InstanceFieldMap.CreateInstance(
                ResourceType.Utp,
                blueprint,
                PlaceableResRef,
                4f,
                6f,
                0f,
                0f,
                1f);
            source.SetString("Tag", GffFieldType.CExoString, "copied_source_tag");
            source.SetString(
                "COPY_SENTINEL", GffFieldType.CExoString, "preserve the full instance");
            placeables.InsertElement(placeables.Elements!.Count, source);
            File.WriteAllBytes(gitPath, git.ToBytes());

            var gicPath = Path.Combine(_moduleRoot, "gic", $"{AreaResRef}.gic.json");
            var gicJson = JsonGffDocument.Load(gicPath);
            var gic = new GicDocument(gicJson);
            gic.InsertBlankComment(
                "Placeable List", ResourceType.Utp, 0, expectedCount: 1);
            GicDocument.SetComment(gic.Placeables[0], "builder note survives copy");
            File.WriteAllBytes(gicPath, gicJson.ToBytes());
        }

        private async Task<AreaEditorViewModel> CreateEditorAsync(
            AreaInstanceClipboard? clipboard = null, ModuleEntryEditor? moduleEntry = null)
        {
            var log = new OutputLogService();
            var context = new WorkspaceContext(_ => throw new NotSupportedException(), log);
            var resources = new ResourceIndex(null, Array.Empty<ResourceIndex.HakLayer>());
            await resources.InitializationTask;
            return new AreaEditorViewModel(
                AreaResRef,
                new ModuleWorkspace(_moduleRoot),
                new LookupOptionProvider(context),
                gameCodeIndex: null,
                log,
                tilesetCatalog: new TilesetCatalog(resources),
                tileModelCache: new TileModelCache(resources),
                resourceIndex: resources,
                prompts: new StubPrompts(),
                instanceClipboard: clipboard,
                moduleEntry: moduleEntry);
        }

        private static void AssertFacingNorth(Vector2 orientation)
        {
            orientation.X.Should().BeApproximately(0f, 0.00001f);
            orientation.Y.Should().BeApproximately(1f, 0.00001f);
        }

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline)
                    Assert.Fail("Timed out waiting for the initial area scene build.");

                await Task.Delay(25);
            }
        }

        private sealed class StubPrompts : IEditorPromptService
        {
            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string name) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string path) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<string?> PromptForTextAsync(
                string headline,
                string message,
                string initialValue,
                string confirmLabel) =>
                Task.FromResult<string?>(null);

            public Task<bool> ConfirmDestructiveAsync(
                string headline,
                string message,
                string confirmLabel) =>
                Task.FromResult(false);
        }

        private sealed class ShortcutAreaEditorView : AreaEditorView
        {
            public bool SendControlKey(Key key) => SendKey(key, KeyModifiers.Control);

            public bool SendKey(Key key, KeyModifiers modifiers = KeyModifiers.None, object? source = null)
            {
                var args = new KeyEventArgs
                {
                    RoutedEvent = KeyDownEvent,
                    Source = source,
                    Key = key,
                    KeyModifiers = modifiers
                };
                OnKeyDown(args);
                return args.Handled;
            }
        }
    }
}
