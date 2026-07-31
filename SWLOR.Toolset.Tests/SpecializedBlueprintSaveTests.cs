using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
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
