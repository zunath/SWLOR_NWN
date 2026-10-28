using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ItemDefinition;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.SpaceObjectDefinition;
using SWLOR.Game.Server.Service.FactionAgentService;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Rift Runs (plan section 31): the Rift Keys and where they drop, the pocket template and who enters it, the chambers
/// in sequence, the timer, the payout and the collapse. Numbers come from tools/SpaceRevampBalance/events.py
/// (RiftRunTuning).
/// </summary>
public class RiftRunTests
{
    private static readonly DateTime Start = new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

    private static string Root()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        return directory!.FullName;
    }

    private static string ReadSource(params string[] relativePath) =>
        File.ReadAllText(relativePath.Aggregate(Path.Combine(Root(), "SWLOR.Game.Server"), Path.Combine));

    private static JsonDocument Module(string kind, string resref) =>
        JsonDocument.Parse(File.ReadAllBytes(Path.Combine(Root(), "Module", kind, $"{resref}.{kind}.json")));

    private static Func<int, int> Rolls(params int[] values)
    {
        var index = 0;
        return max => Math.Min(values[Math.Min(index++, values.Length - 1)], max - 1);
    }

    private static RiftRun Run(int tierTenths = 30, int ships = 1) =>
        new() { Key = RiftRunTuning.Keys.Single(x => x.TierTenths == tierTenths), Ships = ships, StartedAt = Start, NextAt = Start.AddSeconds(5) };

    // Keys

    [Test]
    public void Keys_ComeInTiersOneToFiveAndFivePointFive()
    {
        RiftRunTuning.Keys.Select(x => x.TierTenths).Should().Equal(10, 20, 30, 40, 50, 55);
        RiftRunTuning.Keys.Select(x => x.Resref).Should().Equal("rift_key_t1", "rift_key_t2", "rift_key_t3", "rift_key_t4", "rift_key_t5", "rift_key_t55");
        RiftRunTuning.Keys.Last().Resref.Should().Be(WildSpaceTuning.RiftKeyResref);
        RiftRunTuning.Keys.Select(x => x.TierLabel).Should().Equal("1", "2", "3", "4", "5", "5.5");
        RiftRunTuning.Keys.Select(x => x.CreaturePrefix).Should().Equal("t1", "t2", "t3", "t4", "t5", "rr");
        RiftRunTuning.Keys.Last().LootTable.Should().Be("SPACE_ELITE_WS");

        var models = new HashSet<int>();
        foreach (var key in RiftRunTuning.Keys)
        {
            using var uti = Module("uti", key.Resref);
            var root = uti.RootElement;
            root.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be(key.Name);
            root.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(24, "a miscellaneous small item, as the tier 5.5 key");
            models.Add(root.GetProperty("ModelPart1").GetProperty("value").GetInt32()).Should().BeTrue("each key has its own keycard model");
            root.TryGetProperty("VarTable", out _).Should().BeFalse("keys drop and are sold, so they are not NO_ECONOMY");
            var property = root.GetProperty("PropertiesList").GetProperty("value").EnumerateArray().Single();
            property.GetProperty("PropertyName").GetProperty("value").GetInt32().Should().Be(15, "an Activate Item property to use it");
            root.GetProperty("Cost").GetProperty("value").GetInt32().Should().BePositive();
            key.BaseCredits.Should().BePositive();
        }

        new RiftKeyItemDefinition().BuildItems().Keys.Should().BeEquivalentTo(RiftRunTuning.Keys.Select(x => x.Resref));
    }

    [Test]
    public void Keys_DropFromAcesWildSpaceAndSalvageAndAreSoldAtFactionStores()
    {
        RiftRunRules.KeyForTier(1).Resref.Should().Be("rift_key_t1");
        RiftRunRules.KeyForTier(5).Resref.Should().Be("rift_key_t5");
        RiftRunRules.KeyForTier(6).Resref.Should().Be("rift_key_t55");
        RiftRunRules.KeyForTier(3, true).Resref.Should().Be("rift_key_t55");
        RiftRunRules.NextKey(RiftRunRules.KeyForTier(4))!.Resref.Should().Be("rift_key_t5");
        RiftRunRules.NextKey(RiftRunRules.KeyForTier(5))!.Resref.Should().Be("rift_key_t55");
        RiftRunRules.NextKey(RiftRunRules.KeyForTier(6)).Should().BeNull();

        // Aces: their tier's key in their own table.
        var aceTables = new SpaceAceLootTableDefinition().BuildLootTables();
        foreach (var ace in SWLOR.Game.Server.Feature.ShipRewardDefinition.RevampAceCatalog.Aces)
            aceTables[ace.LootTable].Should().Contain(x => x.Resref == RiftRunRules.KeyForTier(ace.Tier).Resref, ace.Tag);
        foreach (var key in RiftRunTuning.Keys.Take(5))
            aceTables.Values.SelectMany(x => x).Should().Contain(x => x.Resref == key.Resref, "every tier 1-5 key has an ace source");

        // Wild Space: the W12c tables already drop the tier 5.5 key.
        new SpaceLootTableDefinition().BuildLootTables()["SPACE_BASIC_WS_RARES"].Should().Contain(x => x.Resref == "rift_key_t55");

        // Salvage: after every other rare-find share (elite design 20, ship 5, torpedo 0), the next 5 are the key.
        ShipRareFindRules.Roll(3, WreckDifficulty.Elite, Rolls(25)).Should().Equal("rift_key_t3");
        ShipRareFindRules.Roll(3, WreckDifficulty.Elite, Rolls(29)).Should().Equal("rift_key_t3");
        ShipRareFindRules.Roll(3, WreckDifficulty.Elite, Rolls(30)).Should().BeEmpty();
        ShipRareFindRules.Roll(6, WreckDifficulty.Elite, Rolls(27)).Should().Equal("rift_key_t55");
        // Boss: design 25, ship 10, torpedo 15, then the key 10.
        ShipRareFindRules.Roll(2, WreckDifficulty.Boss, Rolls(55)).Should().Equal("rift_key_t2");
        ShipRareFindRules.Roll(2, WreckDifficulty.Boss, Rolls(60)).Should().BeEmpty();
        // Wild Space: after the Module Schematic and Prototype Tech shares.
        ShipRareFindRules.Roll(5, WreckDifficulty.Boss, Rolls(75), true).Should().Equal("rift_key_t55");

        // Faction stores: each faction sells the keys up to its highest dock tier, gated by standing that rises with the tier.
        FactionAgentTuning.StoreRiftKeys[FactionType.Czerka].Should().Equal("rift_key_t1", "rift_key_t2", "rift_key_t3", "rift_key_t55");
        FactionAgentTuning.StoreRiftKeys[FactionType.RepublicMilitary].Should().Equal(RiftRunTuning.Keys.Select(x => x.Resref));
        ((int)FactionAgentTuning.RiftKeyGates["rift_key_t1"]).Should().BeLessThan((int)FactionAgentTuning.RiftKeyGates["rift_key_t5"]);
        var store = FactionAgentRules.GetStoreItems(FactionType.SithMilitary).Where(x => x.Category == FactionTokenStoreCategory.RiftKey).ToList();
        store.Select(x => x.Resref).Should().Equal(FactionAgentTuning.StoreRiftKeys[FactionType.SithMilitary]);
        store.Should().OnlyContain(x => x.Tokens == FactionAgentTuning.RiftKeyPrices[x.Resref] && x.MinimumTier == FactionAgentTuning.RiftKeyGates[x.Resref]);
    }

    // The pocket

    [Test]
    public void Pocket_IsATemplateAreaWithThreeChambers()
    {
        using (var git = Module("git", RiftRunTuning.TemplateResref))
        {
            var locals = git.RootElement.GetProperty("VarTable").GetProperty("value").EnumerateArray()
                .Select(x => x.GetProperty("Name").GetProperty("value").GetString()).ToList();
            locals.Should().Equal("SPACE");
            var waypoints = git.RootElement.GetProperty("WaypointList").GetProperty("value").EnumerateArray()
                .ToDictionary(x => x.GetProperty("Tag").GetProperty("value").GetString()!, x => x);
            waypoints.Keys.Should().BeEquivalentTo("STUCK_WAYPOINT", "RIFT_ROOM_1", "RIFT_ROOM_2", "RIFT_ROOM_3");
            for (var room = 0; room < 3; room++)
            {
                var waypoint = waypoints[$"{RiftRunTuning.RoomWaypointPrefix}{room + 1}"];
                waypoint.GetProperty("XPosition").GetProperty("value").GetSingle().Should().Be(RiftRunTuning.RoomPoints[room].X);
                waypoint.GetProperty("YPosition").GetProperty("value").GetSingle().Should().Be(RiftRunTuning.RoomPoints[room].Y);
            }

            git.RootElement.GetProperty("Creature List").GetProperty("value").GetArrayLength().Should().Be(0);
        }

        using (var are = Module("are", RiftRunTuning.TemplateResref))
            are.RootElement.GetProperty("ResRef").GetProperty("value").GetString().Should().Be(RiftRunTuning.TemplateResref);
        File.Exists(Path.Combine(Root(), "Module", "gic", $"{RiftRunTuning.TemplateResref}.gic.json")).Should().BeTrue();
        File.ReadAllText(Path.Combine(Root(), "Module", "ifo", "module.ifo.json"), System.Text.Encoding.Latin1).Should().Contain("\"space_riftpocket\"");

        for (var i = 0; i < RiftRunTuning.RoomPoints.Count; i++)
        for (var j = i + 1; j < RiftRunTuning.RoomPoints.Count; j++)
        {
            var (a, b) = (RiftRunTuning.RoomPoints[i], RiftRunTuning.RoomPoints[j]);
            Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2)).Should().BeGreaterThan(100, "chambers stand far apart");
        }

        ReadSource("Service", "Area.cs").Should().Contain("AreasByResref.Remove(SpaceService.RiftRunTuning.TemplateResref);");
        var runs = ReadSource("Service", "SpaceRiftRuns.cs");
        runs.Should().Contain("Area.CreateInstance(RiftRunTuning.TemplateResref").And.Contain("DestroyArea(run.Area)")
            .And.Contain("Item.ReduceItemStack(item, 1);");
    }

    [Test]
    public void Party_IsTheUserAndUpToTwoNearbyShips()
    {
        RiftRunRules.PickParty(1u, Array.Empty<(uint, float)>()).Should().Equal(1u);
        RiftRunRules.PickParty(1u, new (uint, float)[] { (2, 10f), (3, 31f), (4, 5f), (5, 20f), (1, 0f) })
            .Should().Equal(1u, 4u, 2u);
        RiftRunRules.PickParty(1u, new (uint, float)[] { (2, RiftRunTuning.JoinMeters) }).Should().Equal(1u, 2u);
        RiftRunTuning.MaxShips.Should().Be(3);

        var key = RiftRunRules.KeyForTier(2);
        RiftRunRules.GetStartRefusal(key, true, false, false, false, 0).Should().Be(RiftRunRefusal.None);
        RiftRunRules.GetStartRefusal(null, true, false, false, false, 0).Should().Be(RiftRunRefusal.UnknownKey);
        RiftRunRules.GetStartRefusal(key, false, false, false, false, 0).Should().Be(RiftRunRefusal.NotPiloting);
        RiftRunRules.GetStartRefusal(key, true, false, true, false, 0).Should().Be(RiftRunRefusal.InCombat);
        RiftRunRules.GetStartRefusal(key, true, false, false, true, 0).Should().Be(RiftRunRefusal.AlreadyInRun);
        RiftRunRules.GetStartRefusal(key, true, true, false, false, 0).Should().Be(RiftRunRefusal.Cloaked);
        RiftRunRules.GetStartRefusal(key, true, false, false, false, RiftRunTuning.MaxRuns).Should().Be(RiftRunRefusal.TooManyRuns);
        RiftRunRules.Message(RiftRunRefusal.InCombat).Should().NotBeEmpty();
    }

    // Chambers

    [Test]
    public void Chambers_ScaleWithTheParty()
    {
        var key = RiftRunRules.KeyForTier(4);
        RiftRunRules.RoomWaves(key, 0, 1).Should().BeEquivalentTo(new[] { new[] { "fighter", "interceptor" }, new[] { "fighter", "interceptor" } },
            o => o.WithStrictOrdering());
        RiftRunRules.RoomWaves(key, 1, 3)[0].Should().Equal("sniper", "ewar", "interceptor", "interceptor");
        RiftRunRules.RoomWaves(key, 2, 2).Single().Should().Equal("platform", "gunship");
        RiftRunRules.WaveTags(key, 2, 0, 1).Should().Equal("t4platform");
        var raiders = RiftRunRules.KeyForTier(6);
        RiftRunRules.RoomWaves(raiders, 0, 1).Should().HaveCount(4).And.OnlyContain(x => x.Count == 1, "Rift Raiders come one at a time");
        RiftRunRules.WaveTags(raiders, 2, 1, 2).Should().Equal("rrlogistics", "rrgunship");
        RiftRunRules.WaveTags(key, 0, 5, 1).Should().BeEmpty();

        var presets = RevampNpcShipCatalog.Ships.Select(x => x.CreatureTag).ToHashSet();
        foreach (var k in RiftRunTuning.Keys)
        for (var room = 0; room < RiftRunTuning.Rooms.Count; room++)
        for (var ships = 1; ships <= RiftRunTuning.MaxShips; ships++)
        for (var wave = 0; wave < RiftRunRules.RoomWaves(k, room, ships).Count; wave++)
            presets.Should().Contain(RiftRunRules.WaveTags(k, room, wave, ships), $"{k.Resref} room {room} wave {wave}");
    }

    [Test]
    public void Chambers_RunInSequenceAndTheRunPays()
    {
        var run = Run();
        var t = Start;
        RiftRunRules.Step(run, 0, 1, t).Should().Be(RiftRunAction.None, "the first wave comes after the delay");
        t = run.NextAt;
        RiftRunRules.Step(run, 0, 1, t).Should().Be(RiftRunAction.SpawnWave);
        RiftRunRules.Step(run, 2, 1, t.AddSeconds(30)).Should().Be(RiftRunAction.None, "the wave still fights");

        var rooms = new List<int>();
        for (var guard = 0; guard < 50 && run.Phase != RiftRunPhase.Returning; guard++)
        {
            t = t.AddSeconds(30);
            var action = RiftRunRules.Step(run, 0, 1, t);
            if (action == RiftRunAction.PullToNextRoom)
                rooms.Add(run.Room);
            if (run.Phase is RiftRunPhase.Waiting or RiftRunPhase.Advancing && t < run.NextAt)
                t = run.NextAt;
            if (run.Phase == RiftRunPhase.Waiting)
                RiftRunRules.Step(run, 0, 1, t).Should().Be(RiftRunAction.SpawnWave);
        }

        rooms.Should().Equal(1, 2);
        run.Phase.Should().Be(RiftRunPhase.Returning);
        run.FinishedSecondsLeft.Should().Be(RiftRunRules.SecondsLeft(Start, t));
        RiftRunRules.Step(run, 0, 1, t).Should().Be(RiftRunAction.None);
        RiftRunRules.Step(run, 0, 1, run.NextAt).Should().Be(RiftRunAction.ReturnHome);
        run.Phase.Should().Be(RiftRunPhase.Closing);
        RiftRunRules.Step(run, 0, 1, run.NextAt.AddSeconds(5)).Should().Be(RiftRunAction.None);
    }

    [Test]
    public void Timer_WarnsAndTheCollapseEjectsEveryone()
    {
        RiftRunRules.SecondsLeft(Start, Start).Should().Be(RiftRunTuning.TimerSeconds);
        RiftRunRules.SecondsLeft(Start, Start.AddMinutes(14)).Should().Be(60);
        RiftRunRules.SecondsLeft(Start, Start.AddMinutes(20)).Should().Be(0);
        RiftRunRules.FormatTimer(605).Should().Be("10:05");

        var run = Run();
        RiftRunRules.DueWarnings(run, 400).Should().BeEmpty();
        RiftRunRules.DueWarnings(run, 299).Should().Equal(300);
        RiftRunRules.DueWarnings(run, 250).Should().BeEmpty("each warning once");
        RiftRunRules.DueWarnings(run, 9).Should().Equal(new[] { 10 }, "only the most urgent of several due at once");

        var fighting = Run();
        fighting.Phase = RiftRunPhase.Fighting;
        RiftRunRules.Step(fighting, 3, 2, Start.AddSeconds(RiftRunTuning.TimerSeconds)).Should().Be(RiftRunAction.Collapse);
        fighting.Phase.Should().Be(RiftRunPhase.Closing);

        var empty = Run();
        RiftRunRules.Step(empty, 0, 0, Start.AddSeconds(2)).Should().NotBe(RiftRunAction.Abandon, "ships are still arriving");
        RiftRunRules.Step(empty, 0, 0, Start.AddSeconds(RiftRunRules.ArrivalGraceSeconds)).Should().Be(RiftRunAction.Abandon);

        var runs = ReadSource("Service", "SpaceRiftRuns.cs");
        var collapse = runs.Substring(runs.IndexOf("private static void CollapseRiftPocket", StringComparison.Ordinal));
        collapse.Substring(0, collapse.IndexOf("Log.Write", StringComparison.Ordinal)).Should().Contain("EffectDeath()",
            "destroying the ship runs the normal escape pods and XP debt (Space.ApplyDeath)");
        ReadSource("Service", "SpaceDeath.cs").Should().Contain("Death.ApplyPenalties");
    }

    [Test]
    public void Payout_ScalesWithTierAndTimeLeft()
    {
        var key = RiftRunRules.KeyForTier(3);
        RiftRunRules.Multiplier(0).Should().Be(RiftRunTuning.MinimumMultiplier);
        RiftRunRules.Multiplier(RiftRunTuning.TimerSeconds).Should().Be(RiftRunTuning.MinimumMultiplier + RiftRunTuning.TimeSlope);
        RiftRunRules.Credits(key, 450).Should().Be((int)Math.Round(key.BaseCredits * (RiftRunTuning.MinimumMultiplier + RiftRunTuning.TimeSlope / 2), MidpointRounding.AwayFromZero));
        RiftRunRules.Credits(key, 600).Should().BeGreaterThan(RiftRunRules.Credits(key, 100));
        RiftRunRules.Pulls(0).Should().Be(RiftRunTuning.RunPulls);
        RiftRunRules.Pulls(RiftRunTuning.TimerSeconds / 2).Should().Be(RiftRunTuning.RunPulls + 1);

        RiftRunTuning.Keys.Select(x => x.BaseCredits).Should().OnlyContain(x => x > 0);
        RiftRunTuning.Keys.Last().BaseCredits.Should().BeGreaterThan(RiftRunTuning.Keys.First().BaseCredits);

        RiftRunRules.ShareCredits(100, new[] { "pilot", "gunner", "engineer" }).Should().Equal(("pilot", 34), ("gunner", 33), ("engineer", 33));
        RiftRunRules.ShareCredits(100, Array.Empty<string>()).Should().BeEmpty();
    }

    [Test]
    public void ThePlan_ListsWhatTheUpdateAdded()
    {
        var plan = ReadSource("Readmes", "SpaceRevampPlan.md");
        var section = plan.Substring(plan.IndexOf("## 31. Rift Runs", StringComparison.Ordinal));
        section.Substring(0, section.IndexOf("## 32.", StringComparison.Ordinal)).Should().Contain("Added by the incursions and Rift Runs update (W15b)");
    }
}
