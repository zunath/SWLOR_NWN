using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>Verifies GitDocument and GicDocument against Module/git|gic/bank.*.json.</summary>
    public class GitDocumentTests
    {
        private static string BankGitPath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "git", "bank.git.json");

        [Test]
        public void BankGit_ListCounts_MatchCorpus()
        {
            var document = GitDocument.Load(BankGitPath);

            document.Creatures.Should().BeEmpty();
            document.Doors.Should().BeEmpty();
            document.Placeables.Should().HaveCount(1);
            document.Sounds.Should().BeEmpty();
            document.Stores.Should().BeEmpty();
            document.Triggers.Should().BeEmpty();
            document.Waypoints.Should().HaveCount(6);
        }

        [Test]
        public void BankGit_ItemList_IsTheRootListKey_NotSounds()
        {
            // "List" is the loose-item list, not a sound-related list; verify by asserting it
            // reads through the raw "List" field directly.
            var document = GitDocument.Load(BankGitPath);
            document.Items.Should().BeSameAs(document.Fields.Get("List").Elements);
        }

        [Test]
        public void BankGit_Waypoint_KnownTag()
        {
            var document = GitDocument.Load(BankGitPath);
            document.Waypoints[0].Get("Tag").GetString().Should().Be("BANK_TERMINAL_SPAWN");
        }

        [Test]
        public void BankGit_AreaProperties_KnownValue()
        {
            var document = GitDocument.Load(BankGitPath);
            document.AreaProperties.Should().NotBeNull();
            document.AreaProperties!.Get("MusicDay").GetInteger().Should().Be(0);
        }

        [Test]
        public void BankGit_VarTable_HasExpectedEntry()
        {
            var document = GitDocument.Load(BankGitPath);
            document.VarTable.GetInt("EXPLORE_ACHIEVEMENT_ID").Should().Be(48);
        }
    }

    public class GicDocumentTests
    {
        private static string BankGicPath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "gic", "bank.gic.json");

        [Test]
        public void BankGic_ListCounts_MatchCorpus()
        {
            var document = GicDocument.Load(BankGicPath);

            document.Placeables.Should().HaveCount(1);
            document.Waypoints.Should().HaveCount(5);
            document.Creatures.Should().BeEmpty();
        }

        [Test]
        public void TatooineCantinaItemCommentsStayParallelWithItemPlacements()
        {
            var git = GitDocument.Load(Path.Combine(
                CorpusLocator.ModuleDirectory, "git", "tat_anc_cantina.git.json"));
            var gic = GicDocument.Load(Path.Combine(
                CorpusLocator.ModuleDirectory, "gic", "tat_anc_cantina.gic.json"));

            git.Items.Should().HaveCount(2);
            gic.Items.Should().HaveCount(git.Items.Count,
                "GIC comment rows are addressed by the matching GIT placement index");
        }

        [Test]
        public void BankGic_PlaceableComment_ReadsEmptyString()
        {
            var document = GicDocument.Load(BankGicPath);
            GicDocument.GetComment(document.Placeables[0]).Should().Be(string.Empty);
        }

        [Test]
        public void SettingComment_ThenSerializing_RoundTripsTheNewValue()
        {
            var original = File.ReadAllBytes(BankGicPath);
            var document = GicDocument.Parse(original);

            GicDocument.SetComment(document.Placeables[0], "Test comment");
            var written = document.ToBytes();

            var reparsed = GicDocument.Parse(written);
            GicDocument.GetComment(reparsed.Placeables[0]).Should().Be("Test comment");
        }

        [Test]
        public void StructuralCommentEdits_StayParallelAndUndoTogether()
        {
            var original = File.ReadAllBytes(BankGicPath);
            var document = GicDocument.Parse(original);
            using var session = new DocumentSession(BankGicPath, document.Document);
            var startingCount = document.Placeables.Count;

            session.Execute("add paired placeable", () =>
                document.InsertBlankComment(
                    "Placeable List",
                    ResourceType.Utp,
                    startingCount,
                    startingCount + 1));

            document.Placeables.Should().HaveCount(startingCount + 1);
            GicDocument.GetComment(document.Placeables[^1]).Should().BeEmpty();

            session.Undo();
            document.Placeables.Should().HaveCount(startingCount);
            document.ToBytes().Should().Equal(original);
        }
    }
}
