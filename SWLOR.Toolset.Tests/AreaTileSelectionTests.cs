using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Raise and lower act on the selected tile, at once. They used to arm a mode that the next
    /// viewport click resolved, which cost a click per level and gave no way to see which cell was
    /// about to change.
    /// </summary>
    public class AreaTileSelectionTests
    {
        private string _moduleRoot = string.Empty;
        private const string AreaResRef = "cz220shipbreakin";

        [SetUp]
        public void CopyAreaToWorkingModule()
        {
            _moduleRoot = Path.Combine(Path.GetTempPath(), "swlor-tile-selection-" + Guid.NewGuid().ToString("N"));

            // ModuleWorkspace insists on the folders a real module has, so create them all and fill
            // only the ones this area needs.
            foreach (var folder in CorpusLocator.GffFolders)
                Directory.CreateDirectory(Path.Combine(_moduleRoot, folder));

            foreach (var folder in new[] { "are", "git", "gic" })
            {
                var source = Path.Combine(CorpusLocator.ModuleDirectory, folder, $"{AreaResRef}.{folder}.json");
                if (!File.Exists(source))
                    continue;

                var directory = Path.Combine(_moduleRoot, folder);
                Directory.CreateDirectory(directory);
                File.Copy(source, Path.Combine(directory, Path.GetFileName(source)));
            }
        }

        [TearDown]
        public void RemoveWorkingModule()
        {
            if (Directory.Exists(_moduleRoot))
                Directory.Delete(_moduleRoot, recursive: true);
        }

        private AreaEditorViewModel CreateEditor()
        {
            var log = new OutputLogService();
            return new AreaEditorViewModel(
                AreaResRef,
                new ModuleWorkspace(_moduleRoot),
                new LookupOptionProvider(new WorkspaceContext(_ => throw new NotSupportedException(), log)),
                gameCodeIndex: null,
                log,
                prompts: new StubPrompts());
        }

        private sealed class StubPrompts : SWLOR.Toolset.Services.IEditorPromptService
        {
            public Task<SWLOR.Toolset.Services.UnsavedChangesChoice> ConfirmCloseAsync(string name) =>
                Task.FromResult(SWLOR.Toolset.Services.UnsavedChangesChoice.Cancel);

            public Task<SWLOR.Toolset.Services.ExternalChangeChoice> ConfirmExternalChangeAsync(string path) =>
                Task.FromResult(SWLOR.Toolset.Services.ExternalChangeChoice.Cancel);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);

            public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel) =>
                Task.FromResult(false);
        }

        /// <summary>The height level the editor is reporting for the selected tile.</summary>
        private static int SelectedHeight(AreaEditorViewModel editor)
        {
            var status = editor.TileSelectionStatus;
            var marker = status.LastIndexOf("height ", StringComparison.Ordinal);
            marker.Should().BeGreaterThanOrEqualTo(0, "the tile readout names the height: '{0}'", status);
            return int.Parse(status[(marker + "height ".Length)..]);
        }

        [Test]
        public void RaiseAndLowerAreUnavailableUntilATileIsSelected()
        {
            var editor = CreateEditor();

            editor.HasTileSelection.Should().BeFalse();
            editor.RaiseTileCommand.CanExecute(null).Should().BeFalse();
            editor.LowerTileCommand.CanExecute(null).Should().BeFalse();

            editor.SelectTile((3, 6));

            editor.HasTileSelection.Should().BeTrue();
            editor.RaiseTileCommand.CanExecute(null).Should().BeTrue();
            editor.LowerTileCommand.CanExecute(null).Should().BeTrue();
        }

        [Test]
        public void RaiseMovesTheSelectedTileOneLevelPerPress()
        {
            var editor = CreateEditor();
            editor.SelectTile((3, 6));
            var start = SelectedHeight(editor);

            editor.RaiseTileCommand.Execute(null);
            editor.RaiseTileCommand.Execute(null);

            SelectedHeight(editor).Should().Be(
                start + 2, "each press is one level, with no viewport click in between");
            editor.SelectedTile.Should().Be((3, 6), "the tile stays selected so it can be stepped again");
        }

        [Test]
        public void LowerStopsAtTheMinimumHeightAndSaysSo()
        {
            var editor = CreateEditor();
            editor.SelectTile((3, 6));

            // Down to the floor from wherever this tile starts, then one press too many.
            for (var i = 0; i < 8; i++)
                editor.LowerTileCommand.Execute(null);

            SelectedHeight(editor).Should().Be(AreaTiles.MinimumHeightLevel);
            editor.SceneStatus.Should().Contain("minimum height");
        }

        [Test]
        public void SelectingAnInstanceClearsTheTileSelection()
        {
            var editor = CreateEditor();
            editor.SelectTile((3, 6));
            editor.HasTileSelection.Should().BeTrue();

            editor.SelectSceneInstance(new InstanceMarker
            {
                Kind = InstanceMarkerKind.Placeable,
                TemplateResRef = "anything",
                Tag = "ANYTHING",
                Position = Vector3.Zero,
                Orientation = new Vector2(1f, 0f)
            });

            editor.HasTileSelection.Should().BeFalse(
                "raise/lower would have no way to say whether it meant the object or the tile");
            editor.RaiseTileCommand.CanExecute(null).Should().BeFalse();
        }
    }
}
