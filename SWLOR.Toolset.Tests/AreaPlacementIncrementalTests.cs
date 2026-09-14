using System.Numerics;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
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
            AreaInstanceClipboard? clipboard = null)
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
                instanceClipboard: clipboard);
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
            public bool SendControlKey(Key key)
            {
                var args = new KeyEventArgs
                {
                    RoutedEvent = KeyDownEvent,
                    Key = key,
                    KeyModifiers = KeyModifiers.Control
                };
                OnKeyDown(args);
                return args.Handled;
            }
        }
    }
}
