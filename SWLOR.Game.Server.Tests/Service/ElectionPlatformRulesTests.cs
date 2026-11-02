using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// A candidate's platform (Player City Revamp, "Election changes" items 1 to 4): the Edicts it may hold, its cost
/// preview, the City Management warning, what the election window shows for each candidate and the incumbent's record,
/// and which results enact it.
/// </summary>
public class ElectionPlatformRulesTests
{
    private const string City = "home";

    private static ElectionPlatformEdict Promise(EdictType type, string target = "")
    {
        return new ElectionPlatformEdict { Type = type, TargetCityId = target };
    }

    private static (EdictRefusal Refusal, EdictType Type) Check(
        int level,
        bool wildSpace = false,
        params ElectionPlatformEdict[] edicts)
    {
        return ElectionPlatformRules.GetRefusal(edicts, City, level, wildSpace, id => id != "gone");
    }

    private static string LevelName(int level) => level switch
    {
        1 => "Outpost",
        2 => "Village",
        3 => "Township",
        4 => "City",
        _ => "Metropolis"
    };

    private static ElectionPlatformContext Context(
        int level = 4,
        int candidateSupported = 4,
        int baselineSupported = 4,
        int candidateMayorLevel = 5)
    {
        return new ElectionPlatformContext(
            level,
            false,
            12,
            35000,
            20000,
            120000,
            15000,
            2000,
            candidateMayorLevel,
            candidateSupported,
            baselineSupported);
    }

    #region Choosing Edicts

    [Test]
    public void APlatformWithNoEdictsIsAllowed()
    {
        Check(1).Refusal.Should().Be(EdictRefusal.None);
    }

    [TestCase(1, 1)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(4, 2)]
    [TestCase(5, 3)]
    public void APlatformHoldsAsManyEdictsAsTheCityHasSlots(int level, int slots)
    {
        var edicts = new[]
        {
            Promise(EdictType.FestivalSeason),
            Promise(EdictType.GuildCharter),
            Promise(EdictType.OpenGates),
            Promise(EdictType.DrydockSubsidy)
        };

        Check(level, false, edicts.Take(slots).ToArray()).Refusal.Should().Be(EdictRefusal.None);
        Check(level, false, edicts.Take(slots + 1).ToArray()).Should().Be((EdictRefusal.NoFreeSlot, edicts[slots].Type));
    }

    [Test]
    public void AnEdictAboveTheCitysLevelIsRefused()
    {
        Check(2, false, Promise(EdictType.HuntingGrounds)).Should().Be((EdictRefusal.LevelTooLow, EdictType.HuntingGrounds));
        Check(3, false, Promise(EdictType.HuntingGrounds)).Refusal.Should().Be(EdictRefusal.None);
    }

    [Test]
    public void PlanetBasedEdictsAreRefusedOnAWildSpaceStation()
    {
        Check(3, true, Promise(EdictType.ProspectorsCharter)).Should().Be((EdictRefusal.UnavailableHere, EdictType.ProspectorsCharter));
        Check(3, false, Promise(EdictType.ProspectorsCharter)).Refusal.Should().Be(EdictRefusal.None);
        Check(3, true, Promise(EdictType.FestivalSeason)).Refusal.Should().Be(EdictRefusal.None);
    }

    [Test]
    public void AnEdictCannotBeListedTwice()
    {
        Check(5, false, Promise(EdictType.FestivalSeason), Promise(EdictType.FestivalSeason))
            .Should().Be((EdictRefusal.AlreadyHeld, EdictType.FestivalSeason));
    }

    [Test]
    public void AnAllianceOrEmbargoMustNameAnotherLiveCity()
    {
        Check(3, false, Promise(EdictType.Alliance)).Refusal.Should().Be(EdictRefusal.TargetRequired);
        Check(3, false, Promise(EdictType.Embargo, City)).Refusal.Should().Be(EdictRefusal.InvalidTarget, "it cannot name its own city");
        Check(3, false, Promise(EdictType.Embargo, "gone")).Refusal.Should().Be(EdictRefusal.InvalidTarget);
        Check(3, false, Promise(EdictType.Alliance, "hutlar")).Refusal.Should().Be(EdictRefusal.None);
    }

    [Test]
    public void AnAllianceAndAnEmbargoCannotNameTheSameCity()
    {
        Check(5, false, Promise(EdictType.Alliance, "hutlar"), Promise(EdictType.Embargo, "hutlar"))
            .Should().Be((EdictRefusal.ExclusivePair, EdictType.Embargo));
        Check(5, false, Promise(EdictType.Alliance, "hutlar"), Promise(EdictType.Embargo, "veles")).Refusal.Should().Be(EdictRefusal.None);
    }

    [Test]
    public void AnUnknownEdictIsRefused()
    {
        Check(3, false, Promise(EdictType.Invalid)).Should().Be((EdictRefusal.UnknownEdict, EdictType.Invalid));
    }

    [Test]
    public void TheSlotRefusalSaysHowManyEdictsTheLevelHolds()
    {
        ElectionPlatformRules.DescribeRefusal(EdictRefusal.NoFreeSlot, EdictType.FestivalSeason, 3)
            .Should().Be("A platform holds at most 2 Edict(s) at this city level.");
        ElectionPlatformRules.DescribeRefusal(EdictRefusal.LevelTooLow, EdictType.HuntingGrounds, 1)
            .Should().Be("Hunting Grounds needs city level 3.");
    }

    [Test]
    public void Normalize_OrdersByEdictAndKeepsACityOnlyOnEdictsThatNameOne()
    {
        var result = ElectionPlatformRules.Normalize(new[]
        {
            Promise(EdictType.Embargo, " hutlar "),
            Promise(EdictType.FestivalSeason, "stray"),
            null
        });

        result.Select(x => x.Type).Should().Equal(EdictType.FestivalSeason, EdictType.Embargo);
        result.Select(x => x.TargetCityId).Should().Equal(string.Empty, "hutlar");
    }

    [Test]
    public void TheWeeklyCostIsTheSumOfTheEdictsCosts()
    {
        ElectionPlatformRules.GetWeeklyCost(Array.Empty<ElectionPlatformEdict>()).Should().Be(0);
        ElectionPlatformRules.GetWeeklyCost(new[]
        {
            Promise(EdictType.FestivalSeason),
            Promise(EdictType.GuildCharter),
            Promise(EdictType.Embargo, "x")
        }).Should().Be(27500);
    }

    #endregion

    #region Which platform is enacted

    [Test]
    public void AWinnerWithAPlatformEntryEnactsIt()
    {
        var platforms = new Dictionary<string, ElectionPlatform> { ["a"] = new() };

        ElectionPlatformRules.GetPlatformToEnact(platforms, "a").Should().BeSameAs(platforms["a"]);
    }

    [Test]
    public void ARunningWithNoEdictsStillEnactsAnEmptyPlatform()
    {
        var platforms = new Dictionary<string, ElectionPlatform> { ["a"] = new() { Statement = "No promises." } };

        ElectionPlatformRules.GetPlatformToEnact(platforms, "a").Edicts.Should().BeEmpty();
    }

    [Test]
    public void ACandidateWithNoPlatformEntryLeavesTheEdictsAlone()
    {
        var platforms = new Dictionary<string, ElectionPlatform> { ["a"] = new() };

        ElectionPlatformRules.GetPlatformToEnact(platforms, "b").Should().BeNull();
        ElectionPlatformRules.GetPlatformToEnact(null, "a").Should().BeNull();
        ElectionPlatformRules.GetPlatformToEnact(platforms, string.Empty).Should().BeNull();
    }

    [Test]
    public void OnlyAnOutrightWinEnactsAPlatform()
    {
        var platforms = new Dictionary<string, ElectionPlatform>
        {
            ["incumbent"] = new(),
            ["challenger"] = new(),
            ["other"] = new()
        };
        var candidates = new[] { "incumbent", "challenger" };

        ElectionVoter V(string candidate, bool isNo = false) => new() { VoterPlayerId = Guid.NewGuid().ToString(), CandidatePlayerId = candidate, IsNo = isNo };
        IEnumerable<ElectionVoter> Many(string candidate, int count, bool isNo = false) => Enumerable.Range(0, count).Select(_ => V(candidate, isNo));

        var results = new Dictionary<string, (ElectionOutcome Outcome, bool Enacts)>
        {
            ["Yes wins for a challenger"] = (ElectionRules.DecideVoting(ElectionKind.Regular, true, "incumbent", new[] { "challenger" }, Many("challenger", 3).Concat(Many("challenger", 1, true))), true),
            ["Yes wins for the incumbent"] = (ElectionRules.DecideVoting(ElectionKind.Regular, true, "incumbent", new[] { "incumbent" }, Many("incumbent", 2)), true),
            ["No wins against a challenger"] = (ElectionRules.DecideVoting(ElectionKind.Regular, true, "incumbent", new[] { "challenger" }, Many("challenger", 2, true)), false),
            ["No wins against the incumbent"] = (ElectionRules.DecideVoting(ElectionKind.Regular, true, "incumbent", new[] { "incumbent" }, Many("incumbent", 2, true)), false),
            ["Yes and No tie"] = (ElectionRules.DecideVoting(ElectionKind.Regular, true, "incumbent", new[] { "challenger" }, Many("challenger", 1).Concat(Many("challenger", 1, true))), false),
            ["Most votes for a challenger"] = (ElectionRules.DecideVoting(ElectionKind.Regular, false, "incumbent", candidates, Many("challenger", 3).Concat(Many("incumbent", 2))), true),
            ["Most votes for the incumbent"] = (ElectionRules.DecideVoting(ElectionKind.Regular, false, "incumbent", candidates, Many("incumbent", 3).Concat(Many("challenger", 2))), true),
            ["Tie for the most votes"] = (ElectionRules.DecideVoting(ElectionKind.Regular, false, "incumbent", candidates, Many("challenger", 2).Concat(Many("incumbent", 2))), false),
            ["No votes"] = (ElectionRules.DecideVoting(ElectionKind.Regular, false, "incumbent", candidates, Array.Empty<ElectionVoter>()), false),
            ["Every candidate left"] = (ElectionRules.DecideVoting(ElectionKind.Regular, false, "incumbent", Array.Empty<string>(), Array.Empty<ElectionVoter>()), false),
            ["No candidates"] = (ElectionRules.DecideNoCandidates(ElectionKind.Regular, "incumbent"), false),
            ["Snap election, Yes wins"] = (ElectionRules.DecideVoting(ElectionKind.SnapConfidence, true, "incumbent", new[] { "challenger" }, Many("challenger", 2)), true),
            ["Snap election, tie"] = (ElectionRules.DecideVoting(ElectionKind.SnapConfidence, false, "incumbent", candidates, Many("challenger", 1).Concat(Many("incumbent", 1))), false)
        };

        foreach (var (name, (outcome, enacts)) in results)
        {
            var platform = outcome.HasMandate
                ? ElectionPlatformRules.GetPlatformToEnact(platforms, outcome.WinnerPlayerId)
                : null;

            (platform != null).Should().Be(enacts, name);
            if (enacts)
                platform.Should().BeSameAs(platforms[outcome.WinnerPlayerId], name);
        }
    }

    #endregion

    #region Cost preview

    [Test]
    public void TheCostPreviewShowsThePlatformBesideUpkeepTreasuryAndTax()
    {
        var edicts = new[] { Promise(EdictType.FestivalSeason), Promise(EdictType.GuildCharter) };

        var text = ElectionPlatformRules.BuildCostPreview(Context(), edicts);

        text.Should().Contain("Your platform: 25,000 cr a week for 2 Edict(s).");
        text.Should().Contain("Upkeep now: 55,000 cr a week (35,000 base and facilities, 20,000 Edicts).");
        text.Should().Contain("Upkeep with your platform: 60,000 cr a week.");
        text.Should().Contain("Treasury: 120,000 cr, plus 15,000 cr that cannot be withdrawn.");
        text.Should().Contain("Citizenship tax: 2,000 cr from 12 citizens is 24,000 cr a week when everyone pays.");
        text.Should().Contain("first part-week of each Edict");
    }

    [Test]
    public void TheCostPreviewForAnEmptyPlatformCostsNothing()
    {
        var text = ElectionPlatformRules.BuildCostPreview(Context(), Array.Empty<ElectionPlatformEdict>());

        text.Should().Contain("Your platform: 0 cr a week for 0 Edict(s).");
        text.Should().Contain("Upkeep with your platform: 35,000 cr a week.");
        text.Should().NotContain("part-week");
    }

    [Test]
    public void TheCostPreviewLeavesOutTheRestrictedBalanceWhenThereIsNone()
    {
        var context = Context() with { RestrictedTreasury = 0 };

        ElectionPlatformRules.BuildCostPreview(context, Array.Empty<ElectionPlatformEdict>())
            .Should().Contain("Treasury: 120,000 cr.").And.NotContain("cannot be withdrawn");
    }

    [Test]
    public void TheCostPreviewSaysAnAllianceCostsNothingUntilTheOtherCityEnacts()
    {
        ElectionPlatformRules.BuildCostPreview(Context(), new[] { Promise(EdictType.Alliance, "hutlar") })
            .Should().Contain("An Alliance costs nothing until the other city also enacts one.");
    }

    #endregion

    #region City Management warning

    [Test]
    public void NoWarningWhenTheCandidatesRankKeepsTheCityAtItsLevel()
    {
        ElectionPlatformRules.BuildCityLevelWarning(Context(), 2, LevelName).Should().BeEmpty();
    }

    [Test]
    public void NoWarningWhenTheCityWouldDropWhoeverIsMayor()
    {
        var context = Context(level: 4, candidateSupported: 2, baselineSupported: 2, candidateMayorLevel: 2);

        ElectionPlatformRules.BuildCityLevelWarning(context, 2, LevelName).Should().BeEmpty();
    }

    [Test]
    public void TheWarningNamesTheLevelAndSlotsTheCandidatesRankWouldCost()
    {
        var context = Context(level: 4, candidateSupported: 2, baselineSupported: 4, candidateMayorLevel: 2);

        var text = ElectionPlatformRules.BuildCityLevelWarning(context, 1, LevelName);

        text.Should().Contain("supports city level 2 (Village)");
        text.Should().Contain("lowers the city from level 4 (City) to level 2 (Village)");
        text.Should().Contain("Edict slots from 2 to 1");
        text.Should().Contain("not a broken promise");
        text.Should().NotContain("Your platform has", "one Edict still fits");
    }

    [Test]
    public void TheWarningSaysWhenThePlatformHasMoreEdictsThanTheLowerLevelHolds()
    {
        var context = Context(level: 5, candidateSupported: 2, baselineSupported: 5, candidateMayorLevel: 2);

        var text = ElectionPlatformRules.BuildCityLevelWarning(context, 3, LevelName);

        text.Should().Contain("Edict slots from 3 to 1");
        text.Should().Contain("Your platform has 3 Edicts, and only 1 slot(s) would remain.");
    }

    [Test]
    public void TheWarningLeavesOutTheSlotsWhenTheyDoNotChange()
    {
        var context = Context(level: 4, candidateSupported: 3, baselineSupported: 4, candidateMayorLevel: 3);

        var text = ElectionPlatformRules.BuildCityLevelWarning(context, 1, LevelName);

        text.Should().Contain("to level 3 (Township).").And.NotContain("Edict slots from");
    }

    #endregion

    #region Election window text

    [Test]
    public void ACandidateRowShowsTheNameEdictsAndWeeklyCost()
    {
        var platform = new ElectionPlatform();
        platform.Edicts.Add(Promise(EdictType.FestivalSeason));
        platform.Edicts.Add(Promise(EdictType.GuildCharter));

        ElectionPlatformRules.BuildCandidateRowText("Mara", false, platform).Should().Be("Mara - 2 Edict(s), 25,000 cr a week");
        ElectionPlatformRules.BuildCandidateRowText("Mara", true, platform).Should().Be("Mara (incumbent) - 2 Edict(s), 25,000 cr a week");
        ElectionPlatformRules.BuildCandidateRowText("Mara", false, new ElectionPlatform()).Should().Be("Mara - no Edicts");
        ElectionPlatformRules.BuildCandidateRowText("Mara", false, null).Should().Be("Mara - no platform");
    }

    [Test]
    public void TheDetailsListTheEdictsWeeklyCostAndStatement()
    {
        var platform = new ElectionPlatform { Statement = "A busy trade town." };
        platform.Edicts.Add(Promise(EdictType.Alliance, "hutlar"));
        platform.Edicts.Add(Promise(EdictType.OpenGates));

        var text = ElectionPlatformRules.BuildCandidateDetails(
            platform,
            id => id == "hutlar" ? "Hutlar" : string.Empty,
            "No statement.",
            string.Empty);

        text.Should().Be(
            "Edicts promised:\n" +
            "- Alliance naming Hutlar - 5,000 cr a week\n" +
            "- Open Gates - 5,000 cr a week\n" +
            "Weekly cost: 10,000 cr\n\n" +
            "Statement:\nA busy trade town.");
    }

    [Test]
    public void TheDetailsFallBackWhenThereIsNoStatementNoEdictsOrNoPlatform()
    {
        ElectionPlatformRules.BuildCandidateDetails(new ElectionPlatform(), _ => string.Empty, "No statement.", string.Empty)
            .Should().Be("Edicts promised:\nNone\nWeekly cost: 0 cr\n\nStatement:\nNo statement.");
        ElectionPlatformRules.BuildCandidateDetails(null, _ => string.Empty, "No statement.", string.Empty)
            .Should().Be("This candidate made no platform.");
    }

    [Test]
    public void AnEdictNamingACityThatIsGoneSaysSo()
    {
        ElectionPlatformRules.DescribeEdict(Promise(EdictType.Embargo, "gone"), _ => string.Empty)
            .Should().Be("Embargo naming a city that is gone");
    }

    [Test]
    public void TheIncumbentsDetailsEndWithTheirRecordAndRecentDecisions()
    {
        var record = new MayorRecord { Promised = 3, Broken = 1 };
        var decisions = new List<CityGazetteEntry>
        {
            new() { EntryType = CityGazetteEntryType.Decision, TimeUtc = new DateTime(2026, 10, 30, 8, 0, 0, DateTimeKind.Utc), Text = "Sent aid to the docks." }
        };
        var section = ElectionPlatformRules.BuildIncumbentSection(record, decisions);

        section.Should().Be("Promises kept: 2 of 3\nRecent decisions:\n- 2026-10-30: Sent aid to the docks.");
        ElectionPlatformRules.BuildCandidateDetails(new ElectionPlatform(), _ => string.Empty, "None.", section)
            .Should().EndWith("\n\n" + section);
    }

    [Test]
    public void TheIncumbentsRecordSaysWhenThereAreNoDecisionsOrNoPromises()
    {
        ElectionPlatformRules.BuildIncumbentSection(new MayorRecord(), new List<CityGazetteEntry>())
            .Should().Be("Promises kept: no platform promises on record\nRecent decisions: none on the Gazette");
    }

    [TestCase(3, 0, "Promises kept: 3 of 3")]
    [TestCase(3, 1, "Promises kept: 2 of 3")]
    [TestCase(2, 5, "Promises kept: 0 of 2")]
    [TestCase(0, 0, "Promises kept: no platform promises on record")]
    public void TheRecordReadsKeptOfPromised(int promised, int broken, string expected)
    {
        ElectionPlatformRules.BuildRecordText(new MayorRecord { Promised = promised, Broken = broken }).Should().Be(expected);
        ElectionPlatformRules.BuildRecordText(null).Should().Be("Promises kept: no platform promises on record");
    }

    [Test]
    public void RecentDecisionsAreThePublicDecisionsAndEdictChangesNewestFirst()
    {
        CityGazetteEntry Entry(CityGazetteEntryType type, int daysAgo, bool secret = false) => new()
        {
            EntryType = type,
            TimeUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(-daysAgo),
            Text = $"{type} {daysAgo}",
            IsSecret = secret
        };

        var entries = new List<CityGazetteEntry>
        {
            Entry(CityGazetteEntryType.Notice, 0),
            Entry(CityGazetteEntryType.Decision, 5),
            Entry(CityGazetteEntryType.Edict, 1),
            Entry(CityGazetteEntryType.Decision, 2, secret: true),
            Entry(CityGazetteEntryType.Upkeep, 3),
            Entry(CityGazetteEntryType.Decision, 9)
        };

        ElectionPlatformRules.SelectRecentDecisions(entries).Select(x => x.Text)
            .Should().Equal("Edict 1", "Decision 5", "Decision 9");
        ElectionPlatformRules.SelectRecentDecisions(entries, 2).Select(x => x.Text)
            .Should().Equal("Edict 1", "Decision 5");
    }

    #endregion

    #region Saved platforms

    [Test]
    public void APlatformWithEdictsRoundTripsThroughJson()
    {
        var election = new Election();
        election.Platforms["a"] = new ElectionPlatform { Statement = "Hello" };
        election.Platforms["a"].Edicts.Add(Promise(EdictType.Alliance, "hutlar"));
        election.Platforms["a"].Edicts.Add(Promise(EdictType.GuildCharter));

        var copy = Newtonsoft.Json.JsonConvert.DeserializeObject<Election>(Newtonsoft.Json.JsonConvert.SerializeObject(election));

        copy.Platforms["a"].Statement.Should().Be("Hello");
        copy.Platforms["a"].Edicts.Select(x => x.Type).Should().Equal(EdictType.Alliance, EdictType.GuildCharter);
        copy.Platforms["a"].Edicts[0].TargetCityId.Should().Be("hutlar");
        copy.Platforms["a"].Edicts[1].TargetCityId.Should().BeEmpty();
    }

    #endregion
}
