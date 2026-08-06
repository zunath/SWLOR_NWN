using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Creatures;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Editors.Creatures;
using SWLOR.Toolset.Editors.Doors;
using SWLOR.Toolset.Editors.Sounds;
using SWLOR.Toolset.Editors.Triggers;
using SWLOR.Toolset.Editors.Waypoints;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public sealed class SpecializedBlueprintSaveTests
    {
        private string _moduleRoot = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _moduleRoot = Path.Combine(
                Path.GetTempPath(),
                $"swlor-specialized-save-{Guid.NewGuid():N}",
                "Module");
            Directory.CreateDirectory(_moduleRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(Directory.GetParent(_moduleRoot)!.FullName))
                Directory.Delete(Directory.GetParent(_moduleRoot)!.FullName, recursive: true);
        }

        [TestCase(ResourceType.Utw)]
        [TestCase(ResourceType.Utd)]
        [TestCase(ResourceType.Uts)]
        [TestCase(ResourceType.Utt)]
        public async Task SavePersistsBuilderEnteredValues(ResourceType resourceType)
        {
            const string resRef = "save_contract";
            var extension = resourceType.Extension();
            var directory = Path.Combine(_moduleRoot, extension);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"{resRef}.{extension}.json");
            File.WriteAllBytes(
                path,
                BlueprintTemplateFactory.CreateFileContent(
                    resourceType,
                    resRef,
                    "Save Contract"));
            var opened = OpenDocument(
                resourceType,
                path,
                resRef,
                new QuietPrompts());

            try
            {
                opened.EditTag("builder_entered_tag");

                (await opened.Document.TrySaveAsync()).Should().BeTrue();

                JsonGffDocument.Load(path).Root.GetStringOrNull("Tag")
                    .Should().Be("builder_entered_tag");
            }
            finally
            {
                opened.Cleanup();
            }
        }

        [TestCase(ResourceType.Utw)]
        [TestCase(ResourceType.Utd)]
        [TestCase(ResourceType.Uts)]
        [TestCase(ResourceType.Utt)]
        public async Task SaveRefusesAWriteThatLandsAfterOverwriteWasAccepted(
            ResourceType resourceType)
        {
            const string resRef = "save_race";
            var extension = resourceType.Extension();
            var directory = Path.Combine(_moduleRoot, extension);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"{resRef}.{extension}.json");
            File.WriteAllBytes(
                path,
                BlueprintTemplateFactory.CreateFileContent(
                    resourceType,
                    resRef,
                    "Initial generation"));

            var acceptedGeneration = BlueprintTemplateFactory.CreateFileContent(
                resourceType,
                resRef,
                "Accepted external generation");
            var racingGeneration = BlueprintTemplateFactory.CreateFileContent(
                resourceType,
                resRef,
                "Later external generation");
            var prompts = new RacingOverwritePrompts(
                _moduleRoot,
                path,
                racingGeneration);
            var opened = OpenDocument(resourceType, path, resRef, prompts);

            try
            {
                opened.EditTag("builder_edit");
                opened.Document.CanUndo.Should().BeTrue();
                File.WriteAllBytes(path, acceptedGeneration);

                (await opened.Document.TrySaveAsync()).Should().BeFalse(
                    "the later external generation must win instead of being silently overwritten");
                await prompts.WriterFinished.WaitAsync(TimeSpan.FromSeconds(5));

                File.ReadAllBytes(path).Should().Equal(racingGeneration);
                opened.Document.CanUndo.Should().BeTrue(
                    "a rejected save remains dirty so the builder can retry or revert");
            }
            finally
            {
                opened.Cleanup();
            }
        }

        [Test]
        public async Task CreatureSaveRefusesAWriteThatLandsAfterOverwriteWasAccepted()
        {
            const string resRef = "creature_race";
            var directory = Path.Combine(_moduleRoot, "utc");
            Directory.CreateDirectory(directory);
            Directory.CreateDirectory(Path.Combine(_moduleRoot, "uti"));
            var path = Path.Combine(directory, $"{resRef}.utc.json");
            File.WriteAllBytes(
                path,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utc,
                    resRef,
                    "Initial generation"));

            var acceptedGeneration = BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc,
                resRef,
                "Accepted external generation");
            var racingGeneration = BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc,
                resRef,
                "Later external generation");
            var prompts = new RacingOverwritePrompts(
                _moduleRoot,
                path,
                racingGeneration);
            var document = OpenCreature(path, resRef, prompts);

            try
            {
                document.Editor.BasicRows.Single(row => row.Definition.Name == "Tag").Text =
                    "builder_edit";
                File.WriteAllBytes(path, acceptedGeneration);

                (await document.TrySaveAsync()).Should().BeFalse(
                    "the final grouped-save fingerprint check must preserve the later generation");
                await prompts.WriterFinished.WaitAsync(TimeSpan.FromSeconds(5));

                File.ReadAllBytes(path).Should().Equal(racingGeneration);
                document.IsDirty.Should().BeTrue();
                Directory.EnumerateFiles(_moduleRoot, "*.tmp", SearchOption.AllDirectories)
                    .Should().BeEmpty();
            }
            finally
            {
                if (document.IsDirty)
                    document.RevertCommand.Execute(null);
                document.OnClose();
            }
        }

        [Test]
        public async Task CreatureSaveRefusesALateWriteToALinkedItem()
        {
            const string creatureResRef = "linked_race";
            const string weaponResRef = "linked_race_w1";
            var utcDirectory = Path.Combine(_moduleRoot, "utc");
            var utiDirectory = Path.Combine(_moduleRoot, "uti");
            Directory.CreateDirectory(utcDirectory);
            Directory.CreateDirectory(utiDirectory);
            var creaturePath = Path.Combine(utcDirectory, $"{creatureResRef}.utc.json");
            var weaponPath = Path.Combine(utiDirectory, $"{weaponResRef}.uti.json");

            var creature = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Utc,
                creatureResRef,
                "Linked race creature"));
            new CreatureValueStore(creature.Root).SetEquippedResRef(
                CreaturePropertyCatalog.MainWeaponSlot,
                weaponResRef);
            File.WriteAllBytes(creaturePath, creature.ToBytes());

            var initialWeapon = JsonGffDocument.Parse(BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Uti,
                weaponResRef,
                "Initial linked weapon"));
            initialWeapon.Root.SetInt("BaseItem", GffFieldType.Int, 69);
            File.WriteAllBytes(weaponPath, initialWeapon.ToBytes());

            var acceptedGeneration = BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Uti,
                weaponResRef,
                "Accepted linked generation");
            var racingGeneration = BlueprintTemplateFactory.CreateFileContent(
                ResourceType.Uti,
                weaponResRef,
                "Later linked generation");
            var prompts = new RacingOverwritePrompts(
                _moduleRoot,
                weaponPath,
                racingGeneration);
            var document = OpenCreature(creaturePath, creatureResRef, prompts);

            try
            {
                var primary = document.Editor.EquipmentSlots.NaturalWeapons.Single(weapon =>
                    weapon.Label == "Primary Natural Weapon");
                primary.Damage.Number = 12;
                File.WriteAllBytes(weaponPath, acceptedGeneration);

                (await document.TrySaveAsync()).Should().BeFalse(
                    "the final grouped-save check must cover linked UTIs as well as the UTC");
                await prompts.WriterFinished.WaitAsync(TimeSpan.FromSeconds(5));

                File.ReadAllBytes(weaponPath).Should().Equal(racingGeneration);
                document.IsDirty.Should().BeTrue();
            }
            finally
            {
                if (document.IsDirty)
                    document.RevertCommand.Execute(null);
                document.OnClose();
            }
        }

        [Test]
        public async Task CreatureSavePersistsAnEditedNaturalWeaponAfterItIsDisabled()
        {
            const string resRef = "creature_link";
            var directory = Path.Combine(_moduleRoot, "utc");
            Directory.CreateDirectory(directory);
            Directory.CreateDirectory(Path.Combine(_moduleRoot, "uti"));
            var path = Path.Combine(directory, $"{resRef}.utc.json");
            File.WriteAllBytes(
                path,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utc,
                    resRef,
                    "Linked item save"));
            var document = OpenCreature(path, resRef, new QuietPrompts());

            try
            {
                var primary = document.Editor.EquipmentSlots.NaturalWeapons.Single(weapon =>
                    weapon.Label == "Primary Natural Weapon");
                primary.IsEnabled = true;
                var weaponResRef = document.Editor.Equipment.EquippedResRef(
                    CreaturePropertyCatalog.MainWeaponSlot);
                weaponResRef.Should().NotBeNullOrWhiteSpace();

                primary.Damage.Number = 12;
                primary.IsEnabled = false;

                (await document.TrySaveAsync()).Should().BeTrue();
                document.IsDirty.Should().BeFalse();

                var weaponPath = Path.Combine(_moduleRoot, "uti", $"{weaponResRef}.uti.json");
                File.Exists(weaponPath).Should().BeTrue(
                    "saving must not silently discard edits to an item unlinked before save");
                var weapon = new ItemValueStore(JsonGffDocument.Load(weaponPath).Root);
                weapon.GetPropertyValue(CreaturePropertyCatalog.Damage, -1).Should().Be(12);
            }
            finally
            {
                document.OnClose();
            }
        }

        private static CreatureDocumentViewModel OpenCreature(
            string path,
            string resRef,
            IEditorPromptService prompts)
        {
            return new CreatureDocumentViewModel(
                path,
                resRef,
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
        }

        private static OpenedDocument OpenDocument(
            ResourceType resourceType,
            string path,
            string resRef,
            IEditorPromptService prompts)
        {
            var log = new OutputLogService();
            switch (resourceType)
            {
                case ResourceType.Utw:
                {
                    var document = new WaypointDocumentViewModel(
                        path,
                        resRef,
                        gameCodeIndex: null,
                        log,
                        prompts,
                        new WaypointBehaviorCatalog(null, Array.Empty<string>()));
                    return new OpenedDocument(
                        document,
                        value => document.Editor.BasicRows
                            .Concat(document.Editor.BehaviorRows)
                            .Single(row => row.Definition.Name == "Tag").Text = value,
                        () =>
                        {
                            document.RevertCommand.Execute(null);
                            document.OnClose();
                        });
                }
                case ResourceType.Utd:
                {
                    var document = new DoorDocumentViewModel(
                        path, resRef, gameCodeIndex: null, log, prompts);
                    return new OpenedDocument(
                        document,
                        value => document.Editor.BasicRows
                            .Concat(document.Editor.BehaviorRows)
                            .Single(row => row.Definition.Name == "Tag").Text = value,
                        () =>
                        {
                            document.RevertCommand.Execute(null);
                            document.OnClose();
                        });
                }
                case ResourceType.Uts:
                {
                    var document = new SoundDocumentViewModel(
                        path, resRef, gameCodeIndex: null, log, prompts);
                    return new OpenedDocument(
                        document,
                        value => document.Editor.BasicRows
                            .Concat(document.Editor.BehaviorRows)
                            .Single(row => row.Definition.Name == "Tag").Text = value,
                        () =>
                        {
                            document.RevertCommand.Execute(null);
                            document.OnClose();
                        });
                }
                case ResourceType.Utt:
                {
                    var document = new TriggerDocumentViewModel(
                        path, resRef, gameCodeIndex: null, log, prompts);
                    return new OpenedDocument(
                        document,
                        value => document.Editor.BasicRows
                            .Concat(document.Editor.BehaviorRows)
                            .Single(row => row.Definition.Name == "Tag").Text = value,
                        () =>
                        {
                            document.RevertCommand.Execute(null);
                            document.OnClose();
                        });
                }
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(resourceType),
                        resourceType,
                        "The test covers specialized single-file blueprint editors.");
            }
        }

        private sealed record OpenedDocument(
            IEditorDocument Document,
            Action<string> EditTag,
            Action Cleanup);

        private sealed class QuietPrompts : IEditorPromptService
        {
            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<bool> ConfirmDestructiveAsync(
                string headline,
                string message,
                string confirmLabel) =>
                Task.FromResult(false);

            public Task<string?> PromptForTextAsync(
                string headline,
                string message,
                string initialValue,
                string confirmLabel) =>
                Task.FromResult<string?>(null);
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
                        // Give the save continuation ample time to adopt the accepted generation
                        // and block on this lease before the independent writer advances the file.
                        await Task.Delay(1000).ConfigureAwait(false);
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
                string confirmLabel) =>
                Task.FromResult(false);

            public Task<string?> PromptForTextAsync(
                string headline,
                string message,
                string initialValue,
                string confirmLabel) =>
                Task.FromResult<string?>(null);
        }
    }
}
