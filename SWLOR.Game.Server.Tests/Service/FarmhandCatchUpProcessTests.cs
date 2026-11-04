using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.FarmingDefinition;
using SWLOR.Game.Server.Service.FarmhandService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// A farmhand's catch-up across a server stop (Player City Revamp, "Farmhands"). A replay changes the animals or plots and the
/// farmhand's hoppers, which are separate records, and the products the droid collected exist only in the hopper. A fake store stops
/// the catch-up after any number of its writes, as a stop would, and then the next catch-up or the server start finishes it. However it
/// goes, the saved records are exactly what an uninterrupted catch-up leaves: the products are collected once, and no animal or crop
/// is changed twice or lost.
/// </summary>
public class FarmhandCatchUpProcessTests
{
    private const string Owner = "owner";
    private const string Post = "post";
    private const string Assignment = "assignment-1";

    private static readonly DateTime T0 = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Dictionary<CropType, CropDefinition> Crops = new CropListDefinition().BuildCrops();
    private static readonly Dictionary<LivestockType, LivestockDefinition> Animals = new LivestockListDefinition().BuildLivestock();

    private static CropDefinition GetCrop(CropType type) => Crops.GetValueOrDefault(type);

    private static LivestockDefinition GetAnimal(LivestockType type) => Animals.GetValueOrDefault(type);

    private static T Copy<T>(T value) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));

    /// <summary>Thrown by the fake store at the write a server stop interrupts the catch-up at.</summary>
    private sealed class RestartException : Exception
    {
    }

    /// <summary>
    /// The records in memory. Reads hand out the stored instance, as the database's per-frame cache does, so a replay that changed a
    /// record it was given would change what is saved; writes keep a copy, as the database keeps JSON. It counts every write and
    /// stops (throws) at the one a stop interrupts it at. A restart brings every record back as a fresh copy.
    /// </summary>
    private sealed class FakeStore : IFarmhandCatchUpStore
    {
        public Dictionary<string, FarmhandCatchUp> Pending { get; private set; } = new();

        public Dictionary<string, Farmhand> Farmhands { get; private set; } = new();

        public Dictionary<string, Livestock> Livestock { get; private set; } = new();

        public Dictionary<string, GardenPlot> Plots { get; private set; } = new();

        public List<string> Writes { get; } = new();

        public int? StopAfterWrites { get; set; }

        private void Write(string what)
        {
            if (StopAfterWrites.HasValue && Writes.Count >= StopAfterWrites.Value)
                throw new RestartException();

            Writes.Add(what);
        }

        public void Restart()
        {
            Pending = Pending.ToDictionary(pair => pair.Key, pair => Copy(pair.Value));
            Farmhands = Farmhands.ToDictionary(pair => pair.Key, pair => Copy(pair.Value));
            Livestock = Livestock.ToDictionary(pair => pair.Key, pair => Copy(pair.Value));
            Plots = Plots.ToDictionary(pair => pair.Key, pair => Copy(pair.Value));
            StopAfterWrites = null;
            Writes.Clear();
        }

        public FarmhandCatchUp GetPending(string targetId) => Pending.GetValueOrDefault(targetId);

        public void SavePending(FarmhandCatchUp pending)
        {
            Write("pending");
            Pending[pending.TargetId] = Copy(pending);
        }

        public void DeletePending(string targetId)
        {
            Write("unpend");
            Pending.Remove(targetId);
        }

        public Farmhand GetFarmhand(string targetId) => Farmhands.GetValueOrDefault(targetId);

        public List<Livestock> GetAnimals(string corralId) =>
            Livestock.Values.Where(animal => animal.CorralId == corralId).OrderBy(animal => animal.PlacedUtc).ThenBy(animal => animal.Id, StringComparer.Ordinal).ToList();

        public List<GardenPlot> GetPlots(string houseId, string planterId) =>
            Plots.Values.Where(plot => plot.PlanterPlayerId == planterId).OrderBy(plot => plot.Id, StringComparer.Ordinal).ToList();

        public bool AnimalExists(string animalId) => Livestock.ContainsKey(animalId);

        public bool PlotExists(string plotRecordId) => Plots.ContainsKey(plotRecordId);

        public void SetAnimal(Livestock animal)
        {
            Write("animal");
            Livestock[animal.Id] = Copy(animal);
        }

        public void SetPlot(GardenPlot plot)
        {
            Write("plot");
            Plots[plot.Id] = Copy(plot);
        }

        public void RemoveWanderedOff(Livestock animal)
        {
            Write("wander");
            Livestock.Remove(animal.Id);
        }

        public void RemoveWithered(GardenPlot plot, DateTime untilUtc)
        {
            Write("wither");
            Plots.Remove(plot.Id);
        }

        public void SetFarmhand(Farmhand farmhand)
        {
            Write("farmhand");
            Farmhands[farmhand.TargetId] = Copy(farmhand);
        }

        /// <summary>Everything a player could see: the farmhand, the animals and the crops.</summary>
        public string Snapshot() => JsonConvert.SerializeObject(new
        {
            Farmhands = Farmhands.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToList(),
            Livestock = Livestock.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToList(),
            Plots = Plots.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToList()
        });
    }

    #region Scenarios

    private static Farmhand Hand(int tier, FarmhandTargetType target)
    {
        var hand = new Farmhand
        {
            TargetId = Post,
            TargetType = target,
            OwnerPlayerId = Owner,
            DroidName = "Fennec",
            DroidTier = tier,
            DroidAssignmentId = Assignment,
            LastProcessedUtc = T0,
        };
        hand.Id = Farmhand.KeyFor(Post);
        hand.DateCreated = T0;
        return hand;
    }

    private static Livestock Animal(LivestockType type, string id)
    {
        return new Livestock
        {
            Id = id,
            DateCreated = T0,
            OwnerPlayerId = Owner,
            CorralId = Post,
            Species = type,
            PlacedUtc = T0,
            LastProcessedUtc = T0,
        };
    }

    private static GardenPlot Plot(string plotId, double plantedHours)
    {
        var definition = Crops[CropType.ViscaranOrange];
        var planted = T0.AddHours(plantedHours);
        var ripe = FarmingRules.GetRipeUtc(planted, definition.GrowHours);
        return new GardenPlot
        {
            Id = GardenPlot.KeyFor(plotId),
            DateCreated = T0,
            PlotId = plotId,
            PlanterPlayerId = Owner,
            Crop = CropType.ViscaranOrange,
            PlantedUtc = planted,
            TendReadyUtc = FarmingRules.GetTendReadyUtc(planted, definition.GrowHours),
            RipeUtc = ripe,
            WitherUtc = FarmingRules.GetWitherUtc(ripe),
            LastProcessedUtc = planted,
        };
    }

    /// <summary>A corral left four days: the Warocas is fed and collected from, and the Womprat, which a tier 2 droid does not handle, wanders off.</summary>
    private static FakeStore Corral()
    {
        var store = new FakeStore();
        var hand = Hand(2, FarmhandTargetType.Corral);
        hand.FeedHopper[CropType.ViscaranOrange] = 20;
        hand.FeedHopper[CropType.Tomato] = 20;
        store.Farmhands[Post] = hand;
        store.Livestock["warocas"] = Animal(LivestockType.Warocas, "warocas");
        store.Livestock["womprat"] = Animal(LivestockType.Womprat, "womprat");
        return store;
    }

    /// <summary>A house left four days: one crop was tended and withered, another was tended and stands.</summary>
    private static FakeStore House()
    {
        var store = new FakeStore();
        store.Farmhands[Post] = Hand(3, FarmhandTargetType.House);
        var old = Plot("old", 0);
        var young = Plot("young", 60);
        store.Plots[old.Id] = old;
        store.Plots[young.Id] = young;
        return store;
    }

    private static readonly Dictionary<string, Func<FakeStore>> Scenarios = new()
    {
        ["Corral"] = Corral,
        ["House"] = House
    };

    private static readonly DateTime Now = T0.AddHours(96);
    private static readonly DateTime Later = T0.AddHours(130);

    private static FarmhandReplayResult CatchUp(FakeStore store, DateTime now) =>
        FarmhandCatchUpProcess.CatchUp(store, Post, now, GetAnimal, GetCrop);

    /// <summary>What an uninterrupted catch-up at <paramref name="now"/>, and then at <paramref name="then"/> if given, leaves.</summary>
    private static FakeStore Uninterrupted(string scenario, DateTime now, DateTime? then = null)
    {
        var store = Scenarios[scenario]();
        CatchUp(store, now);
        if (then.HasValue)
            CatchUp(store, then.Value);

        return store;
    }

    private static int CountWrites(string scenario)
    {
        var store = Scenarios[scenario]();
        CatchUp(store, Now);
        return store.Writes.Count;
    }

    private static void RunUntilStopped(FakeStore store, int stopAfterWrites, Action run)
    {
        store.StopAfterWrites = stopAfterWrites;
        try
        {
            run();
        }
        catch (RestartException)
        {
        }
    }

    private static IEnumerable<TestCaseData> EveryStop()
    {
        foreach (var scenario in Scenarios.Keys)
        {
            for (var stop = 0; stop <= CountWrites(scenario); stop++)
            {
                yield return new TestCaseData(scenario, stop).SetName($"{scenario}_StoppedAfter{stop}Writes");
            }
        }
    }

    private static IEnumerable<TestCaseData> EveryStopThenEveryStopOfTheRecovery()
    {
        foreach (var scenario in Scenarios.Keys)
        {
            for (var stop = 0; stop <= CountWrites(scenario); stop++)
            {
                for (var recoveryStop = 0; recoveryStop <= CountWrites(scenario); recoveryStop++)
                {
                    yield return new TestCaseData(scenario, stop, recoveryStop).SetName($"{scenario}_StoppedAfter{stop}Writes_RecoveryStoppedAfter{recoveryStop}");
                }
            }
        }
    }

    #endregion

    #region Without a stop

    [Test]
    public void ACorralCatchUp_SavesThePendingRecordFirst_ThenEachRecordWhole_TheFarmhandLast_ThenDeletesThePendingRecord()
    {
        var store = Corral();

        var result = CatchUp(store, Now);

        result.Should().NotBeNull();
        store.Writes.Should().Equal("pending", "animal", "wander", "farmhand", "unpend");
        store.Pending.Should().BeEmpty();
        store.Livestock.Keys.Should().Equal("warocas");
        store.Farmhands[Post].OutputHopper.Should().Equal(new Dictionary<string, int> { ["manure_t1"] = 2 });
        store.Farmhands[Post].LastProcessedUtc.Should().Be(Now);
        store.Livestock["warocas"].LastProcessedUtc.Should().Be(Now);
    }

    [Test]
    public void AHouseCatchUp_SavesThePendingRecordFirst_ThenEachRecordWhole_TheFarmhandLast_ThenDeletesThePendingRecord()
    {
        var store = House();

        CatchUp(store, Now);

        store.Writes.Should().Equal("pending", "plot", "wither", "farmhand", "unpend");
        store.Pending.Should().BeEmpty();
        store.Plots.Keys.Should().Equal(GardenPlot.KeyFor("young"));
        store.Plots[GardenPlot.KeyFor("young")].Tended.Should().BeTrue();
    }

    [Test]
    public void TheReplayRunsOnCopies_SoAStopAfterThePendingRecordLeavesTheStoredRecordsAsTheyWere()
    {
        var store = Corral();
        var before = store.Snapshot();

        RunUntilStopped(store, 1, () => CatchUp(store, Now));

        store.Writes.Should().Equal("pending");
        store.Snapshot().Should().Be(before, "no stored record was changed in place");
        store.Pending[Post].Farmhand.OutputHopper.Should().Equal(new Dictionary<string, int> { ["manure_t1"] = 2 }, "the pending record holds the products");
        store.Pending[Post].Farmhand.LastProcessedUtc.Should().Be(Now);
    }

    [TestCase("Corral")]
    [TestCase("House")]
    public void ACatchUpOfAnEmptySpan_WritesNothing(string scenario)
    {
        var store = Uninterrupted(scenario, Now);
        store.Writes.Clear();

        CatchUp(store, Now).Should().BeNull();
        CatchUp(store, Now.AddHours(-1)).Should().BeNull();

        store.Writes.Should().BeEmpty();
    }

    #endregion

    #region A stop at every write

    [TestCaseSource(nameof(EveryStop))]
    public void AStopAfterAnyWrite_IsFinishedByTheNextCatchUp_WithNothingLostOrCollectedTwice(string scenario, int stop)
    {
        var expected = Uninterrupted(scenario, Now, Later);
        var store = Scenarios[scenario]();

        RunUntilStopped(store, stop, () => CatchUp(store, Now));
        store.Restart();
        CatchUp(store, Later);

        store.Snapshot().Should().Be(expected.Snapshot());
        store.Pending.Should().BeEmpty("no pending record is left to finish again");
    }

    [TestCaseSource(nameof(EveryStop))]
    public void AStopAfterAnyWrite_IsFinishedAtServerStart_WithNothingLostOrCollectedTwice(string scenario, int stop)
    {
        var expected = Uninterrupted(scenario, Now);
        var store = Scenarios[scenario]();

        RunUntilStopped(store, stop, () => CatchUp(store, Now));
        store.Restart();
        FarmhandCatchUpProcess.Recover(store, Post);

        if (stop == 0)
        {
            store.Pending.Should().BeEmpty();
            store.Snapshot().Should().Be(Scenarios[scenario]().Snapshot(), "a stop before the pending record was saved changed nothing");
            return;
        }

        store.Snapshot().Should().Be(expected.Snapshot());
        store.Pending.Should().BeEmpty();
    }

    [TestCaseSource(nameof(EveryStopThenEveryStopOfTheRecovery))]
    public void AStopDuringTheRecoveryItself_IsFinishedByTheNextOne(string scenario, int stop, int recoveryStop)
    {
        var expected = Uninterrupted(scenario, Now, Later);
        var store = Scenarios[scenario]();

        RunUntilStopped(store, stop, () => CatchUp(store, Now));
        store.Restart();
        RunUntilStopped(store, recoveryStop, () => FarmhandCatchUpProcess.Recover(store, Post));
        store.Restart();
        CatchUp(store, Later);

        store.Snapshot().Should().Be(expected.Snapshot());
        store.Pending.Should().BeEmpty();
    }

    [TestCase("Corral")]
    [TestCase("House")]
    public void ARecoveryRunTwice_ChangesNothingTheSecondTime(string scenario)
    {
        var store = Scenarios[scenario]();
        RunUntilStopped(store, 2, () => CatchUp(store, Now));
        store.Restart();

        FarmhandCatchUpProcess.Recover(store, Post).Should().BeTrue();
        var after = store.Snapshot();
        store.Writes.Clear();
        FarmhandCatchUpProcess.Recover(store, Post).Should().BeFalse();

        store.Writes.Should().BeEmpty();
        store.Snapshot().Should().Be(after);
    }

    [Test]
    public void AFarmhandThatAlreadyHoldsTheCatchUp_OnlyHasThePendingRecordDeleted()
    {
        var store = Corral();
        RunUntilStopped(store, 4, () => CatchUp(store, Now));
        store.Restart();
        store.Writes.Should().BeEmpty();
        var after = store.Snapshot();

        FarmhandCatchUpProcess.Recover(store, Post).Should().BeFalse();

        store.Writes.Should().Equal("unpend");
        store.Snapshot().Should().Be(after);
    }

    #endregion

    #region What changed since the stop

    [Test]
    public void AFarmhandReleasedSinceTheStop_IsNotBroughtBack()
    {
        var store = Corral();
        RunUntilStopped(store, 2, () => CatchUp(store, Now));
        store.Restart();
        store.Farmhands.Remove(Post);

        FarmhandCatchUpProcess.Recover(store, Post).Should().BeFalse();
        CatchUp(store, Later).Should().BeNull();

        store.Farmhands.Should().BeEmpty();
        store.Pending.Should().BeEmpty();
        store.Writes.Should().Equal("unpend");
    }

    [Test]
    public void AFarmhandAssignedAgainSinceTheStop_KeepsItsOwnRecords()
    {
        var store = Corral();
        RunUntilStopped(store, 2, () => CatchUp(store, Now));
        store.Restart();
        var next = Hand(2, FarmhandTargetType.Corral);
        next.DroidAssignmentId = "assignment-2";
        next.LastProcessedUtc = Now.AddHours(1);
        store.Farmhands[Post] = next;
        var before = store.Snapshot();

        FarmhandCatchUpProcess.Recover(store, Post).Should().BeFalse();

        store.Snapshot().Should().Be(before, "the earlier droid's catch-up is not written over the new one's records");
        store.Pending.Should().BeEmpty();
    }

    [Test]
    public void AnAnimalRemovedSinceTheStop_IsNotBroughtBack()
    {
        var store = Corral();
        RunUntilStopped(store, 1, () => CatchUp(store, Now));
        store.Restart();
        store.Livestock.Remove("warocas");

        FarmhandCatchUpProcess.Recover(store, Post).Should().BeTrue();

        store.Livestock.Should().NotContainKey("warocas", "it was butchered or crated before the catch-up was finished");
        store.Farmhands[Post].LastProcessedUtc.Should().Be(Now, "the farmhand still gets the rest of the catch-up");
    }

    [Test]
    public void ACropRemovedSinceTheStop_IsNotBroughtBack()
    {
        var store = House();
        RunUntilStopped(store, 1, () => CatchUp(store, Now));
        store.Restart();
        store.Plots.Remove(GardenPlot.KeyFor("young"));

        FarmhandCatchUpProcess.Recover(store, Post).Should().BeTrue();

        store.Plots.Should().BeEmpty("the crop was harvested before the catch-up was finished");
    }

    #endregion

    #region The rules

    [Test]
    public void ThePendingRecord_HoldsTheFullNewStateOfEverythingTheReplayChanged_UnderItsOwnKey()
    {
        var store = Corral();
        RunUntilStopped(store, 1, () => CatchUp(store, Now));

        var pending = store.Pending[Post];

        pending.Id.Should().Be(FarmhandCatchUp.KeyFor(Post)).And.NotBe(Post, "the database's per-frame cache is keyed by id alone");
        pending.AssignmentId.Should().Be(Assignment);
        pending.FromUtc.Should().Be(T0);
        pending.UntilUtc.Should().Be(Now);
        pending.Farmhand.Id.Should().Be(Farmhand.KeyFor(Post));
        pending.Animals.Select(animal => animal.Id).Should().Equal("warocas");
        pending.WanderedOff.Select(animal => animal.Id).Should().Equal("womprat");
        pending.Plots.Should().BeEmpty();
        pending.Withered.Should().BeEmpty();
    }

    [Test]
    public void Settle_AppliesAnUnfinishedCatchUp_FinishesOneTheFarmhandHolds_AndDiscardsOneWithoutItsFarmhand()
    {
        var pending = new FarmhandCatchUp { TargetId = Post, AssignmentId = Assignment, FromUtc = T0, UntilUtc = Now };

        FarmhandCatchUpRules.Settle(pending, new Farmhand { DroidAssignmentId = Assignment, LastProcessedUtc = T0 }).Should().Be(FarmhandCatchUpSettlement.Apply);
        FarmhandCatchUpRules.Settle(pending, new Farmhand { DroidAssignmentId = Assignment, LastProcessedUtc = Now }).Should().Be(FarmhandCatchUpSettlement.Finished);
        FarmhandCatchUpRules.Settle(pending, new Farmhand { DroidAssignmentId = Assignment, LastProcessedUtc = Later }).Should().Be(FarmhandCatchUpSettlement.Finished);
        FarmhandCatchUpRules.Settle(pending, new Farmhand { DroidAssignmentId = "another", LastProcessedUtc = T0 }).Should().Be(FarmhandCatchUpSettlement.Discard);
        FarmhandCatchUpRules.Settle(pending, null).Should().Be(FarmhandCatchUpSettlement.Discard);
    }

    #endregion

    #region Wiring

    private static string ReadServerSource(params string[] path)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;

        root.Should().NotBeNull();
        return File.ReadAllText(Path.Combine(new[] { root!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray()));
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"{signature} should exist");
        var open = source.IndexOf('{', start);
        var depth = 0;
        for (var index = open; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source[start..(index + 1)];
        }

        throw new InvalidOperationException($"{signature} has no closing brace.");
    }

    private static void ShouldComeBefore(string method, string first, string second)
    {
        var a = method.IndexOf(first, StringComparison.Ordinal);
        var b = method.IndexOf(second, StringComparison.Ordinal);
        a.Should().BeGreaterThanOrEqualTo(0, $"'{first}' should be there");
        b.Should().BeGreaterThanOrEqualTo(0, $"'{second}' should be there");
        a.Should().BeLessThan(b, $"'{first}' should come before '{second}'");
    }

    [Test]
    public void TheService_CatchesUpThroughTheProcess_AndFinishesAStoppedOneBeforeAnythingReadsTheAnimals()
    {
        var farmhands = ReadServerSource("Service", "Farmhands.cs");

        var catchUp = ExtractMethod(farmhands, "public static void CatchUp(string targetId)");
        catchUp.Should().Contain("FarmhandCatchUpProcess.CatchUp(").And.NotContain("DB.Set(", "every record is written by the process, whole and in order");
        catchUp.Should().NotContain("FarmhandRules.Replay(", "the process replays on copies");

        ExtractMethod(farmhands, "public static void CatchUpAll()").Should().Contain("RecoverCatchUps();");
        ShouldComeBefore(ExtractMethod(farmhands, "public static void CatchUpAll()"), "RecoverCatchUps();", "FinishDroidReturns();");
        ShouldComeBefore(ExtractMethod(farmhands, "public static void OnPropertyDeleted()"), "RecoverCatchUp(propertyId)", "Get(propertyId)");

        var start = ExtractMethod(ReadServerSource("Service", "Corrals.cs"), "public static void ProcessAtServerStart()");
        ShouldComeBefore(start, "Farmhands.RecoverCatchUps();", "ClerkRecovery.RecoverAnimalsAtServerStart();");
        ShouldComeBefore(start, "Farmhands.RecoverCatchUps();", "Farmhands.CatchUpAll();");

        var store = ExtractMethod(farmhands, "private sealed class CatchUpStore");
        store.Should().Contain("DB.Set(pending)").And.Contain("DB.Set(farmhand)").And.Contain("Corrals.RemoveWanderedOff(animal)");
    }

    [Test]
    public void ThePendingRecord_IsAnIndexedEntityOfItsOwn()
    {
        typeof(FarmhandCatchUp).BaseType.Should().Be(typeof(EntityBase));
        typeof(FarmhandCatchUp).GetProperty(nameof(FarmhandCatchUp.TargetId))!.GetCustomAttributes(typeof(IndexedAttribute), false).Should().NotBeEmpty();
        FarmhandCatchUp.KeyFor("house").Should().NotBe("house");
    }

    #endregion
}
