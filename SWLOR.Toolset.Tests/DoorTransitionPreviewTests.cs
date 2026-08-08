using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Doors;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Editors.Doors;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Transition classification is editor metadata, not a side effect of successfully loading an
    /// MDL. These tests keep the fallback alive through the renderer, placement ghost, and door
    /// preview paths when the authored geometry is unavailable.
    /// </summary>
    [TestFixture]
    public sealed class DoorTransitionPreviewTests
    {
        [Test]
        public void MissingTransitionModel_PreservesResolvedTransitionMetadata()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"door-transition-preview-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "genericdoors.2da"),
                    "2DA V2.0\r\n\r\nLabel StrRef ModelName BlockSight VisibleModel SoundAppType Name\r\n" +
                    "0 Transition 123 missing_transition_model 0 0 **** 123\r\n");
                File.WriteAllText(
                    Path.Combine(scratch, "doortypes.2da"),
                    "2DA V2.0\r\n\r\nLabel Model TileSet TemplateResRef StringRefGame BlockSight VisibleModel SoundAppType\r\n");

                var index = new ResourceIndex(null, Array.Empty<ResourceIndex.HakLayer>());
                index.EnsureInitialized();
                var log = new OutputLogService();
                var context = new WorkspaceContext(path => new ModuleWorkspace(path, index), log);
                var doors = new DoorTypeService(
                    new TwoDaService(scratch),
                    new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));
                var renderer = new BlueprintPreviewRenderer(context, index, doors: doors);
                var door = TransitionDoor();

                var result = renderer.BuildModelResult(ResourceType.Utd, door);
                var appearance = DoorAppearanceCatalog.Read(doors).Should().ContainSingle().Subject;

                result.Model.Should().BeNull("the synthetic transition MDL does not exist");
                result.IsDoorTransition.Should().BeTrue(
                    "the 2DA classification must survive independently of nullable geometry");
                appearance.IsDoorTransition.Should().BeTrue(
                    "the appearance gallery must retain the same classification");
                renderer.RenderModel(appearance.Model!, appearance.IsDoorTransition).Should().NotBeNull(
                    "a missing transition model should render the fixed doorway thumbnail");
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void DoorEditor_MissingTransitionModel_StillCreatesFallbackPreviewMarker()
        {
            using var editor = new DoorEditorViewModel(
                TransitionDoor(),
                "test",
                isInstance: false,
                RunEdit,
                appearances:
                [
                    new DoorAppearanceChoice(
                        DoorAppearanceKind.Generic,
                        0,
                        "Transition",
                        "missing_transition_model",
                        IsDoorTransition: true)
                ],
                resolveModel: _ => new BlueprintModelRenderResult(null, IsDoorTransition: true));

            editor.PreviewScene.Should().NotBeNull();
            var marker = editor.PreviewScene!.Instances.Should().ContainSingle().Subject;
            marker.Model.Should().BeNull();
            marker.IsDoorTransition.Should().BeTrue();
            editor.Appearance.Tiles.Should().ContainSingle()
                .Which.Option.IsDoorTransition.Should().BeTrue();

            var (target, distance) = AreaCameraMath.ComputeSceneFraming(
                editor.PreviewScene,
                AreaSceneBuilder.TileSize,
                MathF.PI / 4f,
                aspectRatio: 1f);
            target.Should().Be(new System.Numerics.Vector3(5f, 5f, 0f));
            distance.Should().BeLessThan(6f,
                "the missing-model doorway fallback should fill its dedicated preview");
        }

        [Test]
        public void PlacementGhost_MissingTransitionModel_StillUsesTransitionFallback()
        {
            var moduleRoot = CopyArea("dan_smugcaverns");
            try
            {
                var log = new OutputLogService();
                var context = new WorkspaceContext(_ => throw new NotSupportedException(), log);
                var editor = new AreaEditorViewModel(
                    "dan_smugcaverns",
                    new ModuleWorkspace(moduleRoot),
                    new LookupOptionProvider(context),
                    gameCodeIndex: null,
                    log,
                    prompts: new StubPrompts(),
                    resolveBlueprintModel: (_, _, _) =>
                        new BlueprintModelRenderResult(
                            null,
                            IsDoorTransition: true,
                            new Dictionary<string, int> { ["TM_test_2"] = 42 }));

                editor.ArmPlacement(
                    ResourceType.Utd,
                    "missing_transition",
                    PaletteSource.Custom).Should().BeTrue();

                editor.PlacementGhost.Should().NotBeNull();
                editor.PlacementGhost!.Model.Should().BeNull();
                editor.PlacementGhost.IsDoorTransition.Should().BeTrue();
                editor.PlacementGhost.TintMapOverrides.Should().Contain("TM_test_2", 42);
            }
            finally
            {
                Directory.Delete(moduleRoot, recursive: true);
            }
        }

        private static Domain.Gff.JsonGffStruct TransitionDoor()
        {
            var door = new ModuleWorkspace(CorpusLocator.ModuleDirectory)
                .LoadBlueprint(ResourceType.Utd, "_mdrn_dt_bars")
                .Fields;
            door.Get("Appearance").SetUnsignedInteger(0);
            door.Get("GenericType_New").SetUnsignedInteger(0);
            return door;
        }

        private static string CopyArea(string resRef)
        {
            var moduleRoot = Path.Combine(
                Path.GetTempPath(),
                $"swlor-transition-placement-{Guid.NewGuid():N}");
            foreach (var folder in CorpusLocator.GffFolders)
                Directory.CreateDirectory(Path.Combine(moduleRoot, folder));

            foreach (var folder in new[] { "are", "git", "gic" })
            {
                var source = Path.Combine(
                    CorpusLocator.ModuleDirectory,
                    folder,
                    $"{resRef}.{folder}.json");
                File.Copy(source, Path.Combine(moduleRoot, folder, Path.GetFileName(source)));
            }

            return moduleRoot;
        }

        private static bool RunEdit(string _, Action mutation)
        {
            mutation();
            return true;
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
    }
}
