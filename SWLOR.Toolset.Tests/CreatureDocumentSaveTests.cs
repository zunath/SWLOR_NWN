using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Creatures;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Editors.Creatures;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    [NonParallelizable]
    public sealed class CreatureDocumentSaveTests
    {
        private string _root = null!;
        private string _moduleRoot = null!;
        private string _path = null!;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"creature-grouped-save-{Guid.NewGuid():N}");
            _moduleRoot = Path.Combine(_root, "Module");
            var utcDirectory = Path.Combine(_moduleRoot, "utc");
            Directory.CreateDirectory(utcDirectory);
            Directory.CreateDirectory(Path.Combine(_moduleRoot, "uti"));
            _path = Path.Combine(utcDirectory, "save_race.utc.json");
            File.WriteAllBytes(
                _path,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utc,
                    "save_race",
                    "Initial generation"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [Test]
        public async Task GroupedSaveRefusesCreatureChangedAfterOverwriteWasAccepted()
        {
            var acceptedGeneration = BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc,
                "save_race",
                "Accepted external generation");
            var racingGeneration = BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc,
                "save_race",
                "Later external generation");
            var prompts = new RacingOverwritePrompts(_moduleRoot, _path, racingGeneration);
            var document = new CreatureDocumentViewModel(
                filePath: _path,
                resRef: "save_race",
                gameCodeIndex: null,
                log: new OutputLogService(),
                prompts: prompts,
                resolveChoices: null,
                resourceIndex: null,
                resolveModel: null,
                appearance: _ => null,
                armorParts: null,
                equipmentChoices: null,
                equipmentDetails: null,
                choicePreviews: null,
                previewAudio: null,
                openLootDefinition: null,
                appearanceOptions: null,
                appearanceThumbnails: null);

            try
            {
                document.Editor.BasicRows.Single(row => row.Definition.Name == "Tag").Text =
                    "builder_edit";
                File.WriteAllBytes(_path, acceptedGeneration);

                (await document.TrySaveAsync()).Should().BeFalse(
                    "the later external generation must win instead of being silently overwritten");
                await prompts.WriterFinished.WaitAsync(TimeSpan.FromSeconds(5));

                File.ReadAllBytes(_path).Should().Equal(racingGeneration);
                document.IsDirty.Should().BeTrue(
                    "a refused grouped save remains dirty so the builder can retry or revert");
            }
            finally
            {
                document.RevertCommand.Execute(null);
                document.OnClose();
            }
        }

        [Test]
        public async Task RevertReloadsLinkedEquipmentAfterSavedHistoryWasBranchedAway()
        {
            var document = new CreatureDocumentViewModel(
                filePath: _path,
                resRef: "save_race",
                gameCodeIndex: null,
                log: new OutputLogService(),
                prompts: new EditorPromptService(),
                resolveChoices: null,
                resourceIndex: null,
                resolveModel: null,
                appearance: _ => null,
                armorParts: null,
                equipmentChoices: null,
                equipmentDetails: null,
                choicePreviews: null,
                previewAudio: null,
                openLootDefinition: null,
                appearanceOptions: null,
                appearanceThumbnails: null);

            try
            {
                var level = document.Editor.Stats.Vitals.Single(cell => cell.Label == "NPC Level");
                level.Number = 7;
                (await document.TrySaveAsync()).Should().BeTrue();

                document.Undo();
                document.Editor.BasicRows.Single(row => row.Definition.Name == "Tag").Text =
                    "branched_edit";
                document.RevertCommand.Execute(null);

                var skin = document.Editor.Equipment.ForSlot(CreaturePropertyCatalog.StatSkinSlot);
                skin.Should().NotBeNull("the saved UTC still references its saved stat skin");
                skin!.Store.GetPropertyValue(CreaturePropertyCatalog.Level, -1).Should().Be(7,
                    "revert must reload linked item bytes, not retain the abandoned undo branch");
                document.IsDirty.Should().BeFalse();
            }
            finally
            {
                document.OnClose();
            }
        }

        private sealed class RacingOverwritePrompts(
            string moduleRoot,
            string path,
            byte[] racingGeneration) : IEditorPromptService
        {
            private readonly TaskCompletionSource<bool> _writerStarted =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private Task? _writer;

            public Task WriterFinished => _writer ?? Task.CompletedTask;

            public async Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath)
            {
                Task writer;
                using (ExecutionContext.SuppressFlow())
                {
                    writer = Task.Run(async () =>
                    {
                        using var moduleWriteLock = ModuleWriteLock.Acquire(moduleRoot);
                        _writerStarted.TrySetResult(true);
                        await Task.Delay(250).ConfigureAwait(false);
                        File.WriteAllBytes(path, racingGeneration);
                    });
                }

                _writer = writer;
                await _writerStarted.Task.ConfigureAwait(false);
                return ExternalChangeChoice.Overwrite;
            }

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<bool> ConfirmDestructiveAsync(
                string headline,
                string message,
                string confirmLabel) => Task.FromResult(false);

            public Task<string?> PromptForTextAsync(
                string headline,
                string message,
                string initialValue,
                string confirmLabel) => Task.FromResult<string?>(null);
        }
    }
}
