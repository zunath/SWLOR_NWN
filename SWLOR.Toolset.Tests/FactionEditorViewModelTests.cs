using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;
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
