using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.ClerkRecoveryService;
using SWLOR.Game.Server.Service.LivestockService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// A clerk action across a restart (Player City Revamp, "Recovery"): the mark goes onto the crop or animal first, the overflow
/// records come second, and the crop or animal is deleted or updated last. A fake store stops the action after any number of its
/// writes, as a restart would, and a second pass over the leftover mark must give each item exactly once.
/// </summary>
public class ClerkRecoveryProcessTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Thrown by the fake store at the write a restart stops the action at.</summary>
    private sealed class RestartException : Exception
    {
    }

    /// <summary>
    /// A crop or animal and the overflow store, in memory. Overflow records are keyed by id, so writing an id twice replaces the
    /// record, as the database does. It counts every write and stops (throws) after <see cref="StopAfterWrites"/> of them.
    /// </summary>
    private sealed class FakeStore : IClerkRecoveryStore
    {
        public ClerkRecoveryMark Mark { get; set; }

        public bool SourceExists { get; set; } = true;

        public int Collections { get; private set; }

        public DateTime? LastCollectedUtc { get; private set; }

        public Dictionary<string, (string Resref, int Count)> Overflow { get; } = new();

        public List<string> Writes { get; } = new();

        public int? StopAfterWrites { get; set; }

        private void Write(string what)
        {
            if (StopAfterWrites.HasValue && Writes.Count >= StopAfterWrites.Value)
                throw new RestartException();

            Writes.Add(what);
        }

        public ClerkRecoveryMark ReadMark() => SourceExists ? Mark : null;

        public void WriteMark(ClerkRecoveryMark mark)
        {
            Write("mark");
            Mark = mark;
        }

        public void WriteOverflowItems(string playerId, string recordPrefix, string resref, int count)
        {
            Write("overflow");
            Overflow[ClerkRecoveryRules.GetOverflowRecordId(recordPrefix, 0)] = (resref, count);
        }

        public void WriteOverflowCrate(string playerId, string recordPrefix, LivestockCrate crate)
        {
            Write("overflow");
            Overflow[ClerkRecoveryRules.GetOverflowRecordId(recordPrefix, 0)] = ("livestock_crate", 1);
        }

        public void FinishSource(ClerkRecoveryMark mark)
        {
            Write("finish");
            if (mark.Action == ClerkRecoveryAction.Collect)
            {
                Collections++;
                LastCollectedUtc = mark.AtUtc;
                Mark = null;
            }
            else
            {
                SourceExists = false;
                Mark = null;
            }
        }

        public int TotalOverflow(string resref) => Overflow.Values.Where(record => record.Resref == resref).Sum(record => record.Count);
    }

    private static ClerkRecoveryMark Harvest() =>
        ClerkRecoveryRules.BuildHarvestMark("harvest-1", "planter", Now, "tomato", 5, "tomato_seed", true);

    private static ClerkRecoveryMark Collect() =>
        ClerkRecoveryRules.BuildProductMark("collect-1", ClerkRecoveryAction.Collect, "owner", Now, new[] { ("manure_t5", 1), ("thune_milk", 1) });

    private static ClerkRecoveryMark Butcher() =>
        ClerkRecoveryRules.BuildProductMark("butcher-1", ClerkRecoveryAction.Butcher, "owner", Now, new[] { ("thune_meat", 6), ("thune_innards", 1) });

    private static ClerkRecoveryMark Crate() =>
        ClerkRecoveryRules.BuildCrateMark("crate-1", "owner", Now, new LivestockCrate { Species = LivestockType.Thune, Feedings = 2, HungerSeconds = 3600 });

    private static IEnumerable<TestCaseData> Actions()
    {
        yield return new TestCaseData((Func<ClerkRecoveryMark>)Harvest).SetName("Harvest");
        yield return new TestCaseData((Func<ClerkRecoveryMark>)Collect).SetName("Collect");
        yield return new TestCaseData((Func<ClerkRecoveryMark>)Butcher).SetName("Butcher");
        yield return new TestCaseData((Func<ClerkRecoveryMark>)Crate).SetName("Crate");
    }

    private static int RecordsOwed(ClerkRecoveryMark mark) => ClerkRecoveryRules.GetItems(mark).Count + (mark.Crate != null ? 1 : 0);

    private static void AssertFinishedOnce(FakeStore store, ClerkRecoveryMark mark)
    {
        store.Overflow.Count.Should().Be(RecordsOwed(mark), "each delivery is one record, however many times it was written");
        foreach (var item in mark.Items)
            store.TotalOverflow(item.Key).Should().Be(item.Value, $"{item.Key} is owed exactly once");
        if (mark.Crate != null)
            store.TotalOverflow("livestock_crate").Should().Be(1);

        store.Mark.Should().BeNull("nothing is left to finish");
        if (mark.Action == ClerkRecoveryAction.Collect)
        {
            store.SourceExists.Should().BeTrue();
            store.Collections.Should().Be(1, "the collection is recorded once");
            store.LastCollectedUtc.Should().Be(mark.AtUtc, "at the time the clerk took it");
        }
        else
        {
            store.SourceExists.Should().BeFalse();
        }
    }

    [TestCaseSource(nameof(Actions))]
    public void WithoutARestart_TheActionGivesEachItemOnceAndEndsTheSource(Func<ClerkRecoveryMark> makeMark)
    {
        var store = new FakeStore();
        var mark = makeMark();

        ClerkRecoveryProcess.Run(store, mark);

        AssertFinishedOnce(store, mark);
        store.Writes.First().Should().Be("mark", "the mark goes onto the crop or animal first");
        store.Writes.Last().Should().Be("finish", "the crop or animal is deleted or updated last");
        store.Writes.Where(write => write == "overflow").Should().HaveCount(RecordsOwed(mark));
    }

    [TestCaseSource(nameof(Actions))]
    public void TheOverflowRecords_AreAlwaysWrittenBeforeTheSourceIsDeletedOrUpdated(Func<ClerkRecoveryMark> makeMark)
    {
        var store = new FakeStore();

        ClerkRecoveryProcess.Run(store, makeMark());

        var lastOverflow = store.Writes.LastIndexOf("overflow");
        var finish = store.Writes.IndexOf("finish");
        lastOverflow.Should().BeGreaterThanOrEqualTo(0);
        lastOverflow.Should().BeLessThan(finish);
    }

    [TestCaseSource(nameof(Actions))]
    public void ARestartAtAnyStep_IsFinishedAtTheNextProcessing_WithNothingLostOrRepeated(Func<ClerkRecoveryMark> makeMark)
    {
        var steps = new FakeStore();
        ClerkRecoveryProcess.Run(steps, makeMark());
        var totalWrites = steps.Writes.Count;

        for (var stopAfter = 0; stopAfter < totalWrites; stopAfter++)
        {
            var store = new FakeStore { StopAfterWrites = stopAfter };
            var mark = makeMark();

            Action run = () => ClerkRecoveryProcess.Run(store, mark);
            run.Should().Throw<RestartException>($"the restart came after {stopAfter} writes");

            // The server comes back: the next processing finds the mark, if the first write got that far, and finishes the action.
            store.StopAfterWrites = null;
            var recovered = ClerkRecoveryProcess.Recover(store);

            if (stopAfter == 0)
            {
                recovered.Should().BeFalse("nothing was written, so nothing is owed and nothing was taken");
                store.Mark.Should().BeNull();
                store.Overflow.Should().BeEmpty();
                store.SourceExists.Should().BeTrue();
                continue;
            }

            recovered.Should().BeTrue($"the mark was written before the restart ({stopAfter} writes)");
            AssertFinishedOnce(store, mark);
        }
    }

    [TestCaseSource(nameof(Actions))]
    public void ProcessingAgainAfterRecovery_DoesNothingNew(Func<ClerkRecoveryMark> makeMark)
    {
        var store = new FakeStore { StopAfterWrites = 2 };
        var mark = makeMark();
        Action run = () => ClerkRecoveryProcess.Run(store, mark);
        run.Should().Throw<RestartException>();
        store.StopAfterWrites = null;

        ClerkRecoveryProcess.Recover(store).Should().BeTrue();
        var writesAfterRecovery = store.Writes.Count;

        ClerkRecoveryProcess.Recover(store).Should().BeFalse("the mark is gone");
        store.Writes.Count.Should().Be(writesAfterRecovery);
        AssertFinishedOnce(store, mark);
    }

    [TestCaseSource(nameof(Actions))]
    public void ARestartBetweenTheOverflowRecordsAndTheFinish_RewritesTheSameRecords(Func<ClerkRecoveryMark> makeMark)
    {
        var mark = makeMark();
        var store = new FakeStore();
        var totalWrites = new Func<int>(() =>
        {
            var probe = new FakeStore();
            ClerkRecoveryProcess.Run(probe, makeMark());
            return probe.Writes.Count;
        })();

        // Stop just before the finish: every overflow record is written, the crop or animal is untouched but marked.
        store.StopAfterWrites = totalWrites - 1;
        Action run = () => ClerkRecoveryProcess.Run(store, mark);
        run.Should().Throw<RestartException>();
        var idsBefore = store.Overflow.Keys.OrderBy(id => id).ToList();
        idsBefore.Should().HaveCount(RecordsOwed(mark));
        store.SourceExists.Should().BeTrue();
        store.Mark.Should().NotBeNull();

        store.StopAfterWrites = null;
        ClerkRecoveryProcess.Recover(store).Should().BeTrue();

        store.Overflow.Keys.OrderBy(id => id).Should().Equal(idsBefore, "the same ids, so the same records are replaced, not added to");
        AssertFinishedOnce(store, mark);
    }

    [Test]
    public void ACropOrAnimalWithNoMark_HasNothingToRecover()
    {
        var store = new FakeStore();

        ClerkRecoveryProcess.Recover(store).Should().BeFalse();
        store.Writes.Should().BeEmpty();
    }

    [Test]
    public void ASourceThatIsGone_HasNothingToRecover()
    {
        var store = new FakeStore { Mark = Harvest(), SourceExists = false };

        ClerkRecoveryProcess.Recover(store).Should().BeFalse();
        store.Writes.Should().BeEmpty();
    }

    [Test]
    public void TheOverflowRecordsOfTwoActionsOnTheSameAnimal_NeverShareAnId()
    {
        var store = new FakeStore();
        var first = ClerkRecoveryRules.BuildProductMark("collect-a", ClerkRecoveryAction.Collect, "owner", Now, new[] { ("manure_t1", 1) });
        var second = ClerkRecoveryRules.BuildProductMark("collect-b", ClerkRecoveryAction.Collect, "owner", Now.AddHours(21), new[] { ("manure_t1", 1) });

        ClerkRecoveryProcess.Run(store, first);
        ClerkRecoveryProcess.Run(store, second);

        store.Overflow.Count.Should().Be(2, "a second collection is a second gift");
        store.TotalOverflow("manure_t1").Should().Be(2);
        store.Collections.Should().Be(2);
    }

    [Test]
    public void AHarvestsItems_AreWrittenInResrefOrder_SoTheirRecordNumbersAreStable()
    {
        var store = new FakeStore();
        var mark = Harvest();

        ClerkRecoveryProcess.Run(store, mark);

        store.Overflow[ClerkRecoveryRules.GetOverflowRecordId(ClerkRecoveryRules.GetOverflowRecordPrefix("harvest-1", 0), 0)]
            .Should().Be(("tomato", 5));
        store.Overflow[ClerkRecoveryRules.GetOverflowRecordId(ClerkRecoveryRules.GetOverflowRecordPrefix("harvest-1", 1), 0)]
            .Should().Be(("tomato_seed", 1));
    }
}
