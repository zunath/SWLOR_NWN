using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.SpaceObjectDefinition;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The capital battle contribution payout (W11p) and the DM Tools Start Capital Battle button. Numbers come from
/// tools/SpaceRevampBalance/events.py (IncursionTuning); the sim line is `python events.py --sim`.
/// </summary>
public class CapitalBattlePayoutTests
{
    private static string Root()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        return directory!.FullName;
    }

    private static string ReadSource(params string[] relativePath) =>
        File.ReadAllText(relativePath.Aggregate(Path.Combine(Root(), "SWLOR.Game.Server"), Path.Combine)).Replace("\r\n", "\n");

    private static IncursionArea Area(string resref) => IncursionTuning.CapitalBattleAreas.Single(x => x.Resref == resref);

    /// <summary>Credits a book of contributions the way the server does, for the whole battle group of an area.</summary>
    private static Dictionary<string, IncursionContribution> FightGroup(IncursionArea area, IncursionInvader invader,
        Func<int, Dictionary<string, double>> damageOnShip, Func<int, string> killer)
    {
        var presets = RevampNpcShipCatalog.Ships.ToDictionary(x => x.CreatureTag);
        var book = new Dictionary<string, IncursionContribution>();
        var ships = CapitalBattleRules.Ships(area, invader);
        for (var index = 0; index < ships.Count; index++)
        {
            var damage = damageOnShip(index);
            var tag = ships[index].Tag;
            var difficulty = presets.TryGetValue(tag, out var preset) ? preset.Difficulty : "Normal";
            IncursionRules.CreditKill(book, area.Tier, damage, IncursionRules.KillPoints(difficulty), killer(index),
                pilot => new List<(string, string)> { (pilot, pilot) });
        }

        return book;
    }

    [TestCase("space_dathomir", 298)]
    [TestCase("dantooineorbit", 298)]
    [TestCase("space_korriban", 817)]
    public void ASoloPilotWhoDestroysTheGroup_IsPaidItsModeledCredits(string resref, int credits)
    {
        var area = Area(resref);
        var book = FightGroup(area, IncursionInvader.Sith, _ => new Dictionary<string, double> { ["solo"] = 500 }, _ => "solo");

        book["solo"].TotalPoints().Should().BeApproximately(11 * 1.1, 1e-9, "a Boss command ship (9) and two Normal gunships (1 each), every killing blow");
        var payout = CapitalBattleRules.BuildPayout(book["solo"], area);
        payout.Credits.Should().Be(credits);
        payout.Pulls.Should().ContainSingle().Which.Should().Be(new KeyValuePair<string, int>($"SPACE_ELITE_T{area.Tier}", 1));
        payout.RegionName.Should().Be(area.Name);
    }

    [Test]
    public void TheCreditsPerPoint_StayInsideTheIncursionBand()
    {
        foreach (var tier in new[] { 5, 6 })
        {
            var battle = IncursionTuning.CapitalBattleCreditsPerPoint[tier];
            var incursion = IncursionTuning.CreditsPerPoint[tier];
            battle.Should().BeGreaterThan(0);
            battle.Should().BeInRange(incursion * 0.9, incursion * 1.1,
                "a battle pays like an incursion minus the region reward's share, so the 100-140% band holds (events.py --sim)");
        }

        IncursionTuning.CapitalBattleCreditsPerPoint.Should().HaveCount(7).And.OnlyContain(x => x >= 0);
    }

    [Test]
    public void PilotsSharingTheKills_ShareTheCredits_AndASlightContributorGetsNoPull()
    {
        var area = Area("space_dathomir");
        var book = FightGroup(area, IncursionInvader.Mandalorian,
            index => index == 0
                ? new Dictionary<string, double> { ["ace"] = 800, ["helper"] = 200, ["scratch"] = 5 }
                : new Dictionary<string, double> { ["ace"] = 100 },
            index => "ace");

        var payouts = book.ToDictionary(x => x.Key, x => CapitalBattleRules.BuildPayout(x.Value, area));
        payouts.Values.Sum(x => x.Credits).Should().BeCloseTo(298, 3, "the group's points are shared, so credits do not multiply");
        payouts["ace"].Credits.Should().BeGreaterThan(payouts["helper"].Credits);
        payouts["ace"].Pulls.Should().NotBeEmpty();
        payouts["helper"].Points.Should().BeGreaterThan(IncursionTuning.CapitalBattleMinimumPoints);
        payouts["helper"].Pulls.Should().NotBeEmpty();
        payouts["scratch"].Points.Should().BeLessThan(IncursionTuning.CapitalBattleMinimumPoints);
        payouts["scratch"].Pulls.Should().BeEmpty("under one kill's worth of points earns no pull");
        payouts["scratch"].Credits.Should().BeGreaterThan(0);
    }

    [Test]
    public void ANoContribution_PaysNothing()
    {
        var payout = CapitalBattleRules.BuildPayout(new IncursionContribution(), Area("space_korriban"));
        payout.Credits.Should().Be(0);
        payout.Pulls.Should().BeEmpty();
        CapitalBattleRules.BuildPayout(null, Area("space_korriban")).Credits.Should().Be(0);
    }

    [Test]
    public void AnOfflinePilotsSpoils_AddUpAcrossBattles()
    {
        var pending = new Dictionary<string, IncursionPayout>();
        var first = new IncursionPayout { Credits = 298, Points = 12.1, RegionName = "Dathomir Orbit", Pulls = { ["SPACE_ELITE_T5"] = 1 } };
        CapitalBattleRules.AddPending(pending, "p1", first);
        CapitalBattleRules.AddPending(pending, "p1", new IncursionPayout
        {
            Credits = 817, Points = 12.1, RegionName = "Korriban Orbit", Pulls = { ["SPACE_ELITE_T6"] = 1 }, Items = { "an_item" },
        });
        CapitalBattleRules.AddPending(pending, "p1", new IncursionPayout { Credits = 100, Points = 2, RegionName = "Dathomir Orbit", Pulls = { ["SPACE_ELITE_T5"] = 1 } });

        pending.Should().ContainSingle();
        var merged = pending["p1"];
        merged.Credits.Should().Be(298 + 817 + 100);
        merged.Points.Should().BeApproximately(26.2, 1e-9);
        merged.Pulls.Should().Equal(new Dictionary<string, int> { ["SPACE_ELITE_T5"] = 2, ["SPACE_ELITE_T6"] = 1 });
        merged.Items.Should().Equal("an_item");

        IncursionRules.ClaimPending(pending, "p1", true)!.Credits.Should().Be(1215);
        pending.Should().BeEmpty();
    }

    [Test]
    public void TheBattle_TracksItsOwnDamageAndPaysOnlyWhenTheGroupIsDestroyed()
    {
        var battles = ReadSource("Service", "SpaceCapitalBattles.cs");
        var destroyed = battles[battles.IndexOf("case CapitalBattleStep.Destroyed:", StringComparison.Ordinal)..];
        destroyed = destroyed[..destroyed.IndexOf("case CapitalBattleStep.Withdraw:", StringComparison.Ordinal)];
        destroyed.IndexOf("PayCapitalBattle()", StringComparison.Ordinal).Should().BeGreaterThan(-1)
            .And.BeLessThan(destroyed.IndexOf("EndCapitalBattle(now)", StringComparison.Ordinal), "the book is cleared by EndCapitalBattle");
        var withdraw = battles[battles.IndexOf("case CapitalBattleStep.Withdraw:", StringComparison.Ordinal)..];
        withdraw = withdraw[..withdraw.IndexOf("EndCapitalBattle(now)", StringComparison.Ordinal)];
        withdraw.Should().Contain("No spoils are paid.").And.NotContain("PayCapitalBattle");

        battles.Should().Contain("DeliverPayout(player, payout,", "an online pilot is paid at once")
            .And.Contain("CapitalBattleRules.AddPending(state.PendingBattlePayouts, playerId, payout)", "an offline pilot's spoils are kept")
            .And.Contain("_capitalBattleContributions.Clear();").And.Contain("_capitalBattleDamage.Clear();");

        var incursions = ReadSource("Service", "SpaceIncursions.cs");
        incursions.Should().Contain("TrackShipDamage(_capitalBattleDamage, _capitalBattleLastHit, _capitalBattlePilotNames, attacker, target, amount);")
            .And.Contain("CreditCapitalBattleKill(ship);");
        battles.Should().Contain("IncursionRules.CreditKill(_capitalBattleContributions, _capitalBattleArea.Tier, damage, points, killer");
    }

    [Test]
    public void ALoginCollectsThePendingBattleSpoils_OnlyOnceItIsDelivered()
    {
        var source = ReadSource("Service", "SpaceCapitalBattles.cs");
        var handler = source[source.IndexOf("public static void DeliverPendingCapitalBattlePayout()", StringComparison.Ordinal)..];
        handler.IndexOf("PendingBattlePayouts.ContainsKey(playerId)", StringComparison.Ordinal).Should().BeGreaterThan(-1);
        handler.IndexOf("IncursionRules.ClaimPending(", StringComparison.Ordinal).Should().BeGreaterThan(handler.IndexOf("DelayCommand(6f", StringComparison.Ordinal));
        handler[..handler.IndexOf("DelayCommand(6f", StringComparison.Ordinal)].Should().NotContain("PendingBattlePayouts.Remove");
        source.Should().Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]");
    }

    // The DM Tools button

    [Test]
    public void TheCapitalBattlePicker_OffersARandomOrbitThenEachBattleOrbit()
    {
        DmSpaceToolRules.CapitalBattleLabels.Should().Equal("Random orbit", "Dathomir Orbit (tier 5)", "Dantooine Orbit (tier 5)", "Korriban Orbit (tier 6)");
        DmSpaceToolRules.GetCapitalBattleArea(0).Should().BeEmpty("a random orbit");
        DmSpaceToolRules.GetCapitalBattleArea(1).Should().Be("space_dathomir");
        DmSpaceToolRules.GetCapitalBattleArea(3).Should().Be("space_korriban");
        DmSpaceToolRules.GetCapitalBattleArea(4).Should().BeEmpty();
        DmSpaceToolRules.GetCapitalBattleArea(-1).Should().BeEmpty();
    }

    [Test]
    public void TheStartCapitalBattleButton_IsWiredAndTheSpaceTabKeepsItsPanel()
    {
        var definition = ReadSource("Feature", "GuiDefinition", "DMToolsDefinition.cs");
        definition.Should().Contain("model.OnStartCapitalBattle()").And.Contain("\"Start Capital Battle\"")
            .And.Contain("BindOptions(model => model.SpaceBattleOptions)").And.Contain("BindSelectedIndex(model => model.SpaceBattleIndex)");
        definition.Should().Contain("private const float SpacePanelWidth = 1000f;").And.Contain("private const float SpacePanelHeight = 470f;");
        definition.Should().NotContain("SetIsEnabled(false)");

        var model = ReadSource("Feature", "GuiDefinition", "ViewModel", "DMToolsViewModel.cs");
        model.Should().Contain("Space.StartCapitalBattleAt(DmSpaceToolRules.GetCapitalBattleArea(SpaceBattleIndex))")
            .And.Contain("WatchOnClient(model => model.SpaceBattleIndex);")
            .And.Contain("SpaceBattleOptions = ComboOptions(DmSpaceToolRules.CapitalBattleLabels);");
        var restore = model[model.IndexOf("private void RestoreTabPicks()", StringComparison.Ordinal)..];
        restore[..restore.IndexOf("}", StringComparison.Ordinal)].Should().Contain("SpaceBattleIndex = SpaceBattleIndex;");

        var battles = ReadSource("Service", "SpaceCapitalBattles.cs");
        battles.Should().Contain("public static (bool Started, string Message) StartCapitalBattleAt(string areaResref)")
            .And.Contain("A capital battle is already under way in").And.Contain("A capital battle waits for it to end.");
    }
}
