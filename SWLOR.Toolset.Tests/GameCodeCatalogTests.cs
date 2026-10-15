using FluentAssertions;
using NUnit.Framework;
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
}
