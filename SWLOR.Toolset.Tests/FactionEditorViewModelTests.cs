using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Factions;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    public sealed class FactionEditorViewModelTests
    {
        [Test]
        public async Task RemovingFactionSavesFacAndEveryAffectedModuleReferenceAsOneWorkflow()
        {
            var moduleRoot = Path.Combine(
                Path.GetTempPath(), "swlor-faction-save-" + Guid.NewGuid().ToString("N"));
            var facDirectory = Path.Combine(moduleRoot, "fac");
            Directory.CreateDirectory(facDirectory);
            var facPath = Path.Combine(facDirectory, "repute.fac.json");
            File.Copy(Path.Combine(CorpusLocator.ModuleDirectory, "fac", "repute.fac.json"), facPath);
            var utcPath = Write(moduleRoot, "utc", "animal.utc.json", """
                {
                  "__data_type": "UTC ",
                  "FactionID": { "type": "dword", "value": 5 }
                }
                """);
            var gitPath = Write(moduleRoot, "git", "test.git.json", """
                {
                  "__data_type": "GIT ",
                  "Creature List": {
                    "type": "list",
                    "value": [
                      {
                        "__struct_id": 0,
                        "FactionID": { "type": "dword", "value": 6 }
                      }
                    ]
                  }
                }
                """);

            try
            {
                var initialCount = FacDocument.Load(facPath).FactionList.Count;
                var usage = FactionReferenceRewriter.ScanUsage(moduleRoot, initialCount);
                using var viewModel = new FactionEditorViewModel(
                    moduleRoot,
                    usage,
                    new OutputLogService(),
                    new EditorPromptService());

                viewModel.SelectedFaction = viewModel.Factions[5];
                viewModel.RequestRemoveFactionCommand.Execute(null);
                viewModel.IsConfirmingRemove.Should().BeTrue();
                viewModel.RemoveDestination.Should().Be("Merchant");
                viewModel.ConfirmRemoveFactionCommand.Execute(null);

                (await viewModel.TrySaveAsync()).Should().BeTrue();

                var savedFac = FacDocument.Load(facPath);
                savedFac.FactionList.Should().HaveCount(initialCount - 1);
                savedFac.FactionList[5].Get("FactionName").GetString().Should().Be("Neutral");
                JsonGffDocument.Load(utcPath).Root.Get("FactionID").GetInteger().Should().Be(3,
                    "members of the removed Animals faction move to its Merchant parent");
                JsonGffDocument.Load(gitPath).Root.Get("Creature List").Elements![0]
                    .Get("FactionID").GetInteger().Should().Be(5,
                        "ids above the removed faction are compacted in placed instances");
                viewModel.ChangedPaths.Should().BeEquivalentTo(new[] { facPath, utcPath, gitPath });
                viewModel.IsDirty.Should().BeFalse();
            }
            finally
            {
                if (Directory.Exists(moduleRoot))
                    Directory.Delete(moduleRoot, recursive: true);
            }
        }

        [Test]
        public async Task SaveRefusesFactionFileChangedWhileWaitingForModuleLease()
        {
            var moduleRoot = Path.Combine(
                Path.GetTempPath(), "swlor-faction-race-" + Guid.NewGuid().ToString("N"));
            var facDirectory = Path.Combine(moduleRoot, "fac");
            Directory.CreateDirectory(facDirectory);
            var facPath = Path.Combine(facDirectory, "repute.fac.json");
            File.Copy(Path.Combine(CorpusLocator.ModuleDirectory, "fac", "repute.fac.json"), facPath);
            var originalBytes = File.ReadAllBytes(facPath);
            var acceptedBytes = originalBytes.Concat([(byte)' ']).ToArray();
            var externalBytes = originalBytes.Concat([(byte)' ', (byte)' ']).ToArray();
            var prompts = new RacingFactionPrompts(moduleRoot, facPath, externalBytes);

            try
            {
                using var viewModel = new FactionEditorViewModel(
                    moduleRoot,
                    FactionReferenceRewriter.ScanUsage(
                        moduleRoot,
                        FacDocument.Load(facPath).FactionList.Count),
                    new OutputLogService(),
                    prompts);
                viewModel.SelectedFaction = viewModel.Factions[5];
                viewModel.RequestRemoveFactionCommand.Execute(null);
                viewModel.ConfirmRemoveFactionCommand.Execute(null);
                File.WriteAllBytes(facPath, acceptedBytes);

                (await viewModel.TrySaveAsync()).Should().BeFalse();
                await prompts.WriterFinished.WaitAsync(TimeSpan.FromSeconds(5));
                File.ReadAllBytes(facPath).Should().Equal(externalBytes);
                viewModel.IsDirty.Should().BeTrue();
            }
            finally
            {
                if (Directory.Exists(moduleRoot))
                    Directory.Delete(moduleRoot, recursive: true);
            }
        }

        private sealed class RacingFactionPrompts(
            string moduleRoot,
            string path,
            byte[] externalBytes) : IEditorPromptService
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
                        File.WriteAllBytes(path, externalBytes);
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

        private static string Write(
            string moduleRoot,
            string directory,
            string fileName,
            string json)
        {
            var path = Path.Combine(moduleRoot, directory, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return path;
        }
    }
}
