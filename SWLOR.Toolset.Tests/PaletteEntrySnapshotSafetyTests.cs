using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    public sealed class PaletteEntrySnapshotSafetyTests
    {
        [Test]
        public async Task RetainedEntryCannotActAfterTypeOrSourceChanges()
        {
            var ownedRoot = Path.Combine(
                Path.GetTempPath(),
                $"swlor_palette_stale_entry_{Guid.NewGuid():N}");
            var moduleRoot = Path.Combine(ownedRoot, "Module");
            foreach (var folder in new[] { "are", "utc", "utp" })
                Directory.CreateDirectory(Path.Combine(moduleRoot, folder));

            var placeablePath = Path.Combine(moduleRoot, "utp", "testplc.utp.json");
            var creaturePath = Path.Combine(moduleRoot, "utc", "testplc.utc.json");
            var originalPlaceable = BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utp,
                "testplc",
                "Test Placeable");
            var originalCreature = BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc,
                "testplc",
                "Test Creature");
            File.WriteAllBytes(placeablePath, originalPlaceable);
            File.WriteAllBytes(creaturePath, originalCreature);

            try
            {
                var log = new OutputLogService();
                var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
                workspace.OpenAndSettle(moduleRoot);
                var prompts = new CountingPrompts();
                var editorFactoryCalls = 0;
                var placementTargetCalls = 0;
                var palette = new PaletteViewModel(
                    workspace,
                    new CategoryService(workspace, log),
                    log,
                    editorService: () =>
                    {
                        editorFactoryCalls++;
                        return null!;
                    },
                    placementTarget: () =>
                    {
                        placementTargetCalls++;
                        return null;
                    },
                    prompts: prompts)
                {
                    SelectedType = ResourceType.Utp
                };
                palette.Refresh();
                palette.PresentationState.SelectedRow = palette.PresentationState.Rows
                    .Single(row => row.Name == "Unsorted");
                var retainedEntry = palette.PresentationState.Tiles
                    .Single(tile => tile.ResRef == "testplc")
                    .Snapshot;

                palette.SelectedType = ResourceType.Utc;
                var callsAfterTypeChange = placementTargetCalls;
                palette.Place(retainedEntry);
                palette.Edit(retainedEntry);
                palette.EditCopy(retainedEntry);
                await palette.DeleteAsync(retainedEntry, CancellationToken.None);
                placementTargetCalls.Should().Be(callsAfterTypeChange);

                palette.SelectedType = ResourceType.Utp;
                palette.Source = PaletteSource.Standard;
                var callsAfterSourceChange = placementTargetCalls;
                palette.Place(retainedEntry);
                palette.Edit(retainedEntry);
                palette.EditCopy(retainedEntry);
                await palette.DeleteAsync(retainedEntry, CancellationToken.None);
                placementTargetCalls.Should().Be(callsAfterSourceChange);

                File.ReadAllBytes(placeablePath).Should().Equal(originalPlaceable);
                File.ReadAllBytes(creaturePath).Should().Equal(originalCreature);
                Directory.GetFiles(Path.Combine(moduleRoot, "utp"), "testplc*", SearchOption.TopDirectoryOnly)
                    .Should().ContainSingle();
                prompts.ConfirmCalls.Should().Be(0);
                editorFactoryCalls.Should().Be(0);
            }
            finally
            {
                if (Directory.Exists(ownedRoot))
                    ScratchDirectory.Delete(ownedRoot);
            }
        }

        [Test]
        public void SharedEntryCapabilitiesFollowWriteLockAndKeepSelection()
        {
            var ownedRoot = Path.Combine(
                Path.GetTempPath(),
                $"swlor_palette_lock_snapshot_{Guid.NewGuid():N}");
            var moduleRoot = Path.Combine(ownedRoot, "Module");
            foreach (var folder in new[] { "are", "utc", "utp" })
                Directory.CreateDirectory(Path.Combine(moduleRoot, folder));

            File.WriteAllBytes(
                Path.Combine(moduleRoot, "utp", "testplc.utp.json"),
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utp,
                    "testplc",
                    "Test Placeable"));

            try
            {
                var log = new OutputLogService();
                var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
                workspace.OpenAndSettle(moduleRoot);
                var mutationLock = new ModuleMutationLock();
                var palette = new PaletteViewModel(
                    workspace,
                    new CategoryService(workspace, log),
                    log,
                    prompts: new CountingPrompts(),
                    mutationLock: mutationLock)
                {
                    SelectedType = ResourceType.Utp
                };
                palette.Refresh();
                var presentation = palette.PresentationState;
                presentation.SelectedRow = presentation.Rows.Single(row => row.Name == "Unsorted");
                presentation.SelectedTile = presentation.Tiles.Single(tile => tile.ResRef == "testplc");

                presentation.SelectedRow.Should().NotBeNull();
                presentation.SelectedTile.Should().NotBeNull();
                presentation.SelectedRow!.Name.Should().Be("Unsorted");
                presentation.SelectedTile!.ResRef.Should().Be("testplc");
                presentation.SelectedTile.Snapshot.Capabilities.CanDelete.Should().BeTrue();

                mutationLock.Set(true);

                presentation.SelectedRow.Should().NotBeNull();
                presentation.SelectedTile.Should().NotBeNull();
                presentation.SelectedRow!.Name.Should().Be("Unsorted");
                presentation.SelectedTile!.ResRef.Should().Be("testplc");
                presentation.SelectedTile.Snapshot.Capabilities.CanDelete.Should().BeFalse();
                presentation.SelectedTile.Snapshot.Capabilities.CanEditCopy.Should().BeFalse();

                mutationLock.Set(false);

                presentation.SelectedRow.Should().NotBeNull();
                presentation.SelectedTile.Should().NotBeNull();
                presentation.SelectedRow!.Name.Should().Be("Unsorted");
                presentation.SelectedTile!.ResRef.Should().Be("testplc");
                presentation.SelectedTile.Snapshot.Capabilities.CanDelete.Should().BeTrue();
                presentation.SelectedTile.Snapshot.Capabilities.CanEditCopy.Should().BeTrue();
            }
            finally
            {
                if (Directory.Exists(ownedRoot))
                    ScratchDirectory.Delete(ownedRoot);
            }
        }

        private sealed class CountingPrompts : IEditorPromptService
        {
            public int ConfirmCalls { get; private set; }

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
                string confirmLabel)
            {
                ConfirmCalls++;
                return Task.FromResult(true);
            }
        }
    }
}
