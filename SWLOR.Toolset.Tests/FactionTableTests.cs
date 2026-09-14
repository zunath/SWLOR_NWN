using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;

namespace SWLOR.Toolset.Tests
{
    public sealed class FactionTableTests
    {
        private string _root = null!;
        private string _path = null!;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "swlor-factions-" + Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_root, "repute.fac.json");
            Directory.CreateDirectory(_root);
            File.Copy(Path.Combine(CorpusLocator.ModuleDirectory, "fac", "repute.fac.json"), _path);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [Test]
        public void RelationshipsUseTheEngineDirection_SourceReactsToTarget()
        {
            using var session = DocumentSession.Open(_path);
            var table = new FactionTable(new FacDocument(session.Document));

            table.GetReputation(sourceId: 1, targetId: 0).Should().Be(0,
                "Hostile reacts hostilely to PC in the corpus FAC");
            table.GetReputation(sourceId: 0, targetId: 1).Should().Be(100,
                "the reverse PC-to-Hostile reaction is deliberately independent");
        }

        [Test]
        public void AddCopiesBothDirectionsFromTheSelectedParent_AndUndoIsByteExact()
        {
            using var session = DocumentSession.Open(_path);
            var table = new FactionTable(new FacDocument(session.Document));
            var original = session.ToBytes();
            var oldCount = table.Count;
            const int parentId = 2;
            var parentOutward = Enumerable.Range(0, oldCount)
                .Select(target => table.GetReputation(parentId, target)).ToArray();
            var parentInward = Enumerable.Range(0, oldCount)
                .Select(source => table.GetReputation(source, parentId)).ToArray();

            var addedId = -1;
            session.Execute("add faction", () => addedId = table.AddFaction("Test Pilots", parentId));

            addedId.Should().Be(oldCount);
            table.Factions[addedId].Should().Match<FactionDefinition>(faction =>
                faction.Name == "Test Pilots" &&
                faction.ParentId == parentId &&
                faction.GlobalEffect &&
                !faction.IsStandard);
            for (var existingId = 0; existingId < oldCount; existingId++)
            {
                table.GetReputation(addedId, existingId).Should().Be(parentOutward[existingId]);
                table.GetReputation(existingId, addedId).Should().Be(parentInward[existingId]);
            }
            table.GetReputation(addedId, addedId).Should().Be(100);

            session.Undo();
            session.ToBytes().Should().Equal(original,
                "adding a faction is a single undoable document transaction");
        }

        [Test]
        public void RemoveCustomFactionCompactsIds_AndUndoRestoresTheOriginalDocument()
        {
            using var session = DocumentSession.Open(_path);
            var fac = new FacDocument(session.Document);
            var table = new FactionTable(fac);
            var original = session.ToBytes();
            var oldCount = table.Count;
            const int removedId = 5;
            var nextFactionName = table.Factions[removedId + 1].Name;
            var parentId = table.Factions[removedId].ParentId;

            var returnedParent = -1;
            session.Execute("remove faction", () => returnedParent = table.RemoveFaction(removedId));

            returnedParent.Should().Be(parentId);
            table.Count.Should().Be(oldCount - 1);
            table.Factions[removedId].Name.Should().Be(nextFactionName);
            fac.RepList.Should().OnlyContain(entry =>
                entry.Get("FactionID1").GetInteger() < table.Count &&
                entry.Get("FactionID2").GetInteger() < table.Count);
            fac.FactionList.Select(entry => entry.StructId).Should()
                .Equal(Enumerable.Range(0, table.Count).Select(id => (uint?)id));

            session.Undo();
            session.ToBytes().Should().Equal(original,
                "removal, reputation cleanup, and id compaction form one undo step");
        }

        [Test]
        public void StandardFactionsCannotBeRemoved()
        {
            using var session = DocumentSession.Open(_path);
            var table = new FactionTable(new FacDocument(session.Document));

            var act = () => session.Execute("remove PC", () => table.RemoveFaction(0));

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*standard factions cannot be removed*");
        }

        [TestCase(0, "Hostile")]
        [TestCase(10, "Hostile")]
        [TestCase(11, "Neutral")]
        [TestCase(89, "Neutral")]
        [TestCase(90, "Friendly")]
        [TestCase(100, "Friendly")]
        public void ReputationThresholdsMatchNwn(int value, string expected)
        {
            FactionTable.DescribeReputation(value).Should().Be(expected);
        }
    }
}
