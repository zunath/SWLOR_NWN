using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;
using SWLOR.Toolset.Domain.Distribution;
using SWLOR.Toolset.Domain.GameData.GameCode;

namespace SWLOR.Toolset.Tests;

public class GameCodeCatalogTests
{
    [Test]
    public void PublishedCatalogKeepsAllEditorFactsWithoutSourceFiles()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) directory = directory.Parent;
        var source = new GameCodeIndex(Path.Combine(directory!.FullName, "SWLOR.Game.Server"));
        source.IsSourceScanAvailable.Should().BeTrue();
        const string revision = "3333333333333333333333333333333333333333";
        var path = Path.Combine(Path.GetTempPath(), "swlor_catalog_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            GameCodeCatalog.Capture(source, revision).Save(path);
            IGameCodeIndex catalog = GameCodeCatalog.Load(path, revision);
            catalog.Should().BeEquivalentTo(source, options => options.ExcludingMissingMembers());
            catalog.IsValidQuestId(source.QuestIds.First()).Should().BeTrue();
            catalog.IsValidSpawnTableId(source.SpawnTableIds.First()).Should().BeTrue();
            Action mismatch = () => GameCodeCatalog.Load(path, new string('4', 40));
            mismatch.Should().Throw<InvalidDataException>().WithMessage("*does not match*");
        }
        finally { File.Delete(path); }
    }
    [TestCase(false)]
    [TestCase(true)]
    public void WorkspaceCatalogRejectsChangedMetadataOrMissingChecksums(bool omitChecksum)
    {
        var root = Directory.CreateTempSubdirectory("swlor-catalog-").FullName;
        try
        {
            const string revision = "3333333333333333333333333333333333333333";
            var catalog = new GameCodeCatalog
            {
                ContentRevision = revision, QuestIds = new() { "quest" }, SpawnTableIds = new() { "spawn" },
                Factions = new() { [1] = "original" }
            };
            var path = Path.Combine(root, WorkspaceManifest.CatalogPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            catalog.Save(path);
            var manifest = new WorkspaceManifest
            {
                ContentRevision = revision, AssetRevision = revision,
                MetadataHashes = new() { [WorkspaceManifest.CatalogPath] = ContentPaths.HashFile(path) }
            };
            File.WriteAllText(Path.Combine(root, WorkspaceManifest.FileName), JsonSerializer.Serialize(manifest));
            GameCodeCatalog.LoadWorkspace(root).Factions[1].Should().Be("original");
            ContentPaths.Capture(root).Should().BeEmpty("generated metadata is separate from editable contribution files");
            if (omitChecksum)
            {
                manifest.MetadataHashes.Clear();
                File.WriteAllText(Path.Combine(root, WorkspaceManifest.FileName), JsonSerializer.Serialize(manifest));
            }
            else
            {
                catalog.Factions[1] = "modified";
                catalog.Save(path);
            }
            Action load = () => GameCodeCatalog.LoadWorkspace(root);
            load.Should().Throw<InvalidDataException>().WithMessage("*checksum*");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

}
