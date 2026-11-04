using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.EdictService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The pure rules of a city's Edicts: the catalog, slots, locks, what is in effect and billed, enactment refusals,
/// the level-drop order, and who gets citizen benefits.
/// </summary>
public class EdictRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static CityEdict Held(DateTime? enactedUtc = null, EdictState state = EdictState.Active, string target = "")
    {
        return new CityEdict
        {
            State = state,
            EnactedUtc = enactedUtc ?? Now.AddDays(-10),
            LockEndsUtc = state == EdictState.Active ? (enactedUtc ?? Now.AddDays(-10)).AddDays(7) : null,
            TargetCityId = target
        };
    }

    private static Dictionary<EdictType, CityEdict> Edicts(params (EdictType Type, CityEdict Edict)[] held)
    {
        return held.ToDictionary(x => x.Type, x => x.Edict);
    }

    #region Catalog

    [Test]
    public void Catalog_HoldsTheTwelveEdictsWithTheirPermanentValues()
    {
        var types = Enum.GetValues(typeof(EdictType)).Cast<EdictType>().Where(x => x != EdictType.Invalid).ToList();

        types.Select(x => (int)x).OrderBy(x => x).Should().Equal(Enumerable.Range(1, 12));
        EdictCatalog.All.Keys.Should().BeEquivalentTo(types);
    }

    [TestCase(EdictType.FestivalSeason, 1, 10000, EdictPillarType.Roleplay)]
    [TestCase(EdictType.Alliance, 1, 5000, EdictPillarType.Diplomacy)]
    [TestCase(EdictType.Embargo, 1, 2500, EdictPillarType.Diplomacy)]
    [TestCase(EdictType.OpenGates, 1, 5000, EdictPillarType.Economy)]
    [TestCase(EdictType.GuildCharter, 1, 15000, EdictPillarType.Pve)]
    [TestCase(EdictType.ProspectorsCharter, 1, 10000, EdictPillarType.Pve)]
    [TestCase(EdictType.DrydockSubsidy, 1, 10000, EdictPillarType.Pve)]
    [TestCase(EdictType.HuntingGrounds, 3, 15000, EdictPillarType.Pve)]
    [TestCase(EdictType.ArtisansCharter, 1, 10000, EdictPillarType.Crafting)]
    [TestCase(EdictType.BreedersCharter, 1, 10000, EdictPillarType.Crafting)]
    [TestCase(EdictType.IndustrialCharter, 3, 5000, EdictPillarType.Industry)]
    [TestCase(EdictType.Shadowport, 1, 10000, EdictPillarType.Economy)]
    public void Catalog_MatchesThePlansCostsLevelsAndPillars(EdictType type, int minimumLevel, int weeklyCost, EdictPillarType pillar)
    {
        var detail = EdictCatalog.Get(type);

        detail.Type.Should().Be(type);
        detail.WeeklyCost.Should().Be(weeklyCost);
        detail.MinimumCityLevel.Should().Be(minimumLevel);
        detail.Pillar.Should().Be(pillar);
        detail.Name.Should().NotBeNullOrWhiteSpace();
        detail.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void Catalog_OnlyAllianceAndEmbargoNameACity()
    {
        EdictCatalog.All.Values.Where(x => x.NamesTargetCity).Select(x => x.Type)
            .Should().BeEquivalentTo(new[] { EdictType.Alliance, EdictType.Embargo });
    }

    [Test]
    public void Catalog_OnlyTheThreePlanetEdictsArePlanetBased()
    {
        EdictCatalog.All.Values.Where(x => x.IsPlanetBased).Select(x => x.Type)
            .Should().BeEquivalentTo(new[] { EdictType.ProspectorsCharter, EdictType.HuntingGrounds, EdictType.IndustrialCharter });
    }

    [Test]
    public void Catalog_CitizenEdictsAreEveryEdictExceptTheAccessOnes()
    {
        EdictCatalog.All.Values.Where(x => !x.BenefitsCitizens).Select(x => x.Type)
            .Should().BeEquivalentTo(new[] { EdictType.Alliance, EdictType.Embargo, EdictType.OpenGates, EdictType.Shadowport });
    }

    [Test]
    public void Catalog_TextIsAsciiSoItReachesTheGameIntact()
    {
        foreach (var detail in EdictCatalog.All.Values)
        {
            detail.Name.Should().MatchRegex("^[\\x20-\\x7E]+$", detail.Type.ToString());
            detail.Description.Should().MatchRegex("^[\\x20-\\x7E]+$", detail.Type.ToString());
        }
    }

    [Test]
    public void WildSpaceStations_DoNotSeePlanetBasedEdicts()
    {
        EdictRules.GetOfferedEdicts(true).Select(x => x.Type)
            .Should().NotContain(new[] { EdictType.ProspectorsCharter, EdictType.HuntingGrounds, EdictType.IndustrialCharter })
            .And.HaveCount(9);
        EdictRules.GetOfferedEdicts(false).Should().HaveCount(12);
        EdictRules.IsAvailableTo(EdictType.FestivalSeason, true).Should().BeTrue();
        EdictRules.IsAvailableTo(EdictType.HuntingGrounds, true).Should().BeFalse();
        EdictRules.IsAvailableTo(EdictType.HuntingGrounds, false).Should().BeTrue();
    }

    #endregion

    #region Slots and levels

    [TestCase(1, 1)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(4, 2)]
    [TestCase(5, 3)]
    public void Slots_FollowTheCityLevel(int level, int slots)
    {
        EdictRules.GetSlots(level).Should().Be(slots);
    }

    [TestCase(EdictType.FestivalSeason, 1, true)]
    [TestCase(EdictType.HuntingGrounds, 2, false)]
    [TestCase(EdictType.HuntingGrounds, 3, true)]
    [TestCase(EdictType.IndustrialCharter, 2, false)]
    [TestCase(EdictType.IndustrialCharter, 5, true)]
    public void MinimumLevel_GatesTheHigherTierEdicts(EdictType type, int level, bool allowed)
    {
        EdictRules.MeetsMinimumLevel(type, level).Should().Be(allowed);
    }

    #endregion

    #region Exclusive pair

    [Test]
    public void AnAllianceAndAnEmbargo_CannotNameTheSameCity()
    {
        var held = Edicts((EdictType.Alliance, Held(target: "hutlar")));

        EdictRules.IsExclusivePair(EdictType.Embargo, "hutlar", held).Should().BeTrue();
        EdictRules.IsExclusivePair(EdictType.Embargo, "veles", held).Should().BeFalse();
        EdictRules.IsExclusivePair(EdictType.Alliance, "hutlar", held).Should().BeFalse("it is the same Edict");
        EdictRules.IsExclusivePair(EdictType.OpenGates, "hutlar", held).Should().BeFalse();
        EdictRules.IsExclusivePair(EdictType.Alliance, "hutlar", Edicts((EdictType.Embargo, Held(target: "hutlar")))).Should().BeTrue();
    }

    #endregion

    #region Proration

    [TestCase(10000, 7.0, 10000)]
    [TestCase(10000, 3.5, 5000)]
    [TestCase(10000, 1.0, 1428)]
    [TestCase(15000, 6.0, 12857)]
    [TestCase(2500, 2.0, 714)]
    [TestCase(10000, 0.0, 0)]
    [TestCase(10000, -2.0, 0)]
    [TestCase(10000, 9.0, 10000)]
    public void Proration_IsTheWeeklyCostTimesTheDaysLeftOverSevenRoundedDown(int cost, double daysLeft, int expected)
    {
        EdictRules.GetProration(cost, Now, Now.AddDays(daysLeft)).Should().Be(expected);
    }

    [Test]
    public void Proration_NeverRoundsUp()
    {
        EdictRules.GetProration(10000, Now, Now.AddDays(1)).Should().Be(1428, "10000 / 7 is 1428.57");
        EdictRules.GetProration(7, Now, Now.AddDays(6.99)).Should().Be(6);
    }

    [Test]
    public void EnactCharge_AnAllianceCostsNothingUntilThePartnerHasOneWaiting()
    {
        var nextCheck = Now.AddDays(3.5);

        EdictRules.GetEnactCharge(EdictType.Alliance, false, Now, nextCheck).Should().Be(0);
        EdictRules.GetEnactCharge(EdictType.Alliance, true, Now, nextCheck).Should().Be(2500);
        EdictRules.GetEnactCharge(EdictType.FestivalSeason, false, Now, nextCheck).Should().Be(5000);
    }

    #endregion

    #region Lock

    [Test]
    public void TheLock_RunsSevenDaysFromTheTimeTheEdictTakesEffect()
    {
        EdictRules.GetLockEndUtc(Now).Should().Be(Now.AddDays(7));
    }

    [Test]
    public void Repeal_IsRefusedUntilTheLockEnds()
    {
        var edict = Held(Now.AddDays(-6.9));

        EdictRules.CanRepeal(edict, Now).Should().BeFalse();
        EdictRules.CanRepeal(edict, Now.AddDays(0.1)).Should().BeTrue("the lock ends exactly at 7 days");
        EdictRules.CanRepeal(edict, Now.AddDays(5)).Should().BeTrue();
    }

    [Test]
    public void Repeal_APendingAllianceHasNoLockToWaitOut()
    {
        EdictRules.CanRepeal(Held(Now, EdictState.Pending, "hutlar"), Now).Should().BeTrue();
    }

    #endregion

    #region In effect and billed

    [Test]
    public void ConditionsAreDerivedFromTheCityRecord()
    {
        EdictRules.GetConditions(0, false, null, null).Should().Be(CityWideCondition.None);
        EdictRules.GetConditions(35000, false, null, null).Should().Be(CityWideCondition.None, "a fresh bill waiting to be paid is not disrepair");
        EdictRules.GetConditions(0, true, null, null).Should().Be(CityWideCondition.None, "a bill that was paid clears disrepair");
        EdictRules.GetConditions(35000, true, null, null).Should().Be(CityWideCondition.Disrepair);
        EdictRules.GetConditions(0, false, Now, null).Should().Be(CityWideCondition.NoConfidence);
        EdictRules.GetConditions(0, false, null, Now).Should().Be(CityWideCondition.Neglect);
        EdictRules.GetConditions(1, true, Now, Now).Should()
            .Be(CityWideCondition.Disrepair | CityWideCondition.NoConfidence | CityWideCondition.Neglect);
    }

    [Test]
    public void AnEdict_WorksWhenNothingPausesIt()
    {
        EdictRules.IsInEffect(Held(), CityWideCondition.None, Now).Should().BeTrue();
    }

    [TestCase(CityWideCondition.Disrepair)]
    [TestCase(CityWideCondition.NoConfidence)]
    [TestCase(CityWideCondition.Neglect)]
    [TestCase(CityWideCondition.Disrepair | CityWideCondition.Neglect)]
    public void AnyCityWideCondition_PausesTheEdict(CityWideCondition condition)
    {
        EdictRules.IsInEffect(Held(), condition, Now).Should().BeFalse();
        EdictRules.IsBilled(Held(), condition).Should().BeFalse();
    }

    [Test]
    public void ThePauseOfALowCityLevel_StopsTheEdictAndItsBilling()
    {
        var edict = Held();
        edict.IsPausedByCityLevel = true;

        EdictRules.IsInEffect(edict, CityWideCondition.None, Now).Should().BeFalse();
        EdictRules.IsBilled(edict, CityWideCondition.None).Should().BeFalse();
    }

    [Test]
    public void ASituationPause_StopsTheEdictButItIsStillBilled()
    {
        var edict = Held();
        edict.SituationPauseEndsUtc = Now.AddHours(10);

        EdictRules.IsInEffect(edict, CityWideCondition.None, Now).Should().BeFalse();
        EdictRules.IsBilled(edict, CityWideCondition.None).Should().BeTrue("the pause is the outcome");
        EdictRules.IsInEffect(edict, CityWideCondition.None, Now.AddHours(10)).Should().BeTrue("the pause expired");
    }

    [Test]
    public void ASituationPauseOnTopOfACityWideCondition_IsNotBilled()
    {
        var edict = Held();
        edict.SituationPauseEndsUtc = Now.AddHours(10);

        EdictRules.IsBilled(edict, CityWideCondition.Disrepair).Should().BeFalse();
    }

    [Test]
    public void APendingAlliance_NeitherWorksNorIsBilled()
    {
        var pending = Held(Now, EdictState.Pending, "hutlar");

        EdictRules.IsInEffect(pending, CityWideCondition.None, Now).Should().BeFalse();
        EdictRules.IsBilled(pending, CityWideCondition.None).Should().BeFalse();
    }

    [Test]
    public void TheWeeklyBill_ListsEachBilledEdictInTypeOrder()
    {
        var paused = Held();
        paused.IsPausedByCityLevel = true;
        var edicts = Edicts(
            (EdictType.GuildCharter, Held()),
            (EdictType.FestivalSeason, Held()),
            (EdictType.HuntingGrounds, paused),
            (EdictType.Alliance, Held(Now, EdictState.Pending, "hutlar")));

        var lines = EdictRules.GetBillLines(edicts, CityWideCondition.None);

        lines.Select(x => x.Type).Should().Equal(EdictType.FestivalSeason, EdictType.GuildCharter);
        lines.Select(x => x.Credits).Should().Equal(10000, 15000);
        lines[0].Name.Should().Be("Festival Season");
        EdictRules.GetBillLines(edicts, CityWideCondition.Disrepair).Should().BeEmpty();
    }

    [Test]
    public void TheUnbilledList_NamesEachPausedEdictAndWhy()
    {
        var level = Held();
        level.IsPausedByCityLevel = true;
        var edicts = Edicts(
            (EdictType.HuntingGrounds, level),
            (EdictType.Alliance, Held(Now, EdictState.Pending, "hutlar")),
            (EdictType.FestivalSeason, Held()));

        EdictRules.GetUnbilledDescriptions(edicts, CityWideCondition.None, Now)
            .Should().Equal("Alliance (waiting for the other city)", "Hunting Grounds (city level too low)");
        EdictRules.GetUnbilledDescriptions(Edicts((EdictType.FestivalSeason, Held())), CityWideCondition.Disrepair, Now)
            .Should().Equal("Festival Season (city in disrepair)");
    }

    #endregion

    #region Enacting

    private static EdictRefusal Refusal(
        EdictType type,
        int level = 3,
        CityWideCondition conditions = CityWideCondition.None,
        Dictionary<EdictType, CityEdict> held = null,
        string target = "",
        bool targetValid = true,
        bool wildSpace = false)
    {
        return EdictRules.GetEnactRefusal(type, "home", target, targetValid, level, wildSpace, conditions, held ?? new Dictionary<EdictType, CityEdict>());
    }

    [Test]
    public void Enacting_IsAllowedForAnOrdinaryEdictWithAFreeSlot()
    {
        Refusal(EdictType.FestivalSeason).Should().Be(EdictRefusal.None);
        Refusal(EdictType.Alliance, target: "hutlar").Should().Be(EdictRefusal.None);
    }

    [Test]
    public void Enacting_IsRefusedForAnUnknownEdict()
    {
        Refusal(EdictType.Invalid).Should().Be(EdictRefusal.UnknownEdict);
    }

    [Test]
    public void Enacting_IsRefusedBelowTheEdictsMinimumLevel()
    {
        Refusal(EdictType.HuntingGrounds, level: 2).Should().Be(EdictRefusal.LevelTooLow);
        Refusal(EdictType.HuntingGrounds, level: 3).Should().Be(EdictRefusal.None);
    }

    [Test]
    public void Enacting_IsRefusedForAPlanetEdictOnAWildSpaceStation()
    {
        Refusal(EdictType.ProspectorsCharter, wildSpace: true).Should().Be(EdictRefusal.UnavailableHere);
        Refusal(EdictType.FestivalSeason, wildSpace: true).Should().Be(EdictRefusal.None);
    }

    [Test]
    public void Enacting_IsRefusedForAnEdictTheCityAlreadyHolds()
    {
        Refusal(EdictType.FestivalSeason, held: Edicts((EdictType.FestivalSeason, Held()))).Should().Be(EdictRefusal.AlreadyHeld);
    }

    [TestCase(CityWideCondition.Disrepair)]
    [TestCase(CityWideCondition.NoConfidence)]
    [TestCase(CityWideCondition.Neglect)]
    public void Enacting_IsRefusedUnderEachCityWideCondition(CityWideCondition condition)
    {
        Refusal(EdictType.FestivalSeason, conditions: condition).Should().Be(EdictRefusal.CityWideCondition);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void Enacting_IsRefusedWhenEverySlotIsTaken(int level)
    {
        var held = new Dictionary<EdictType, CityEdict>();
        var fillers = new[] { EdictType.GuildCharter, EdictType.DrydockSubsidy, EdictType.ArtisansCharter };
        for (var index = 0; index < EdictRules.GetSlots(level); index++)
            held[fillers[index]] = Held();

        Refusal(EdictType.FestivalSeason, level, held: held).Should().Be(EdictRefusal.NoFreeSlot);

        held.Remove(held.Keys.First());
        Refusal(EdictType.FestivalSeason, level, held: held).Should().Be(EdictRefusal.None, "a repeal frees a slot");
    }

    [Test]
    public void Enacting_APausedEdictStillTakesItsSlot()
    {
        var paused = Held();
        paused.IsPausedByCityLevel = true;

        Refusal(EdictType.FestivalSeason, level: 2, held: Edicts((EdictType.GuildCharter, paused))).Should().Be(EdictRefusal.NoFreeSlot);
    }

    [Test]
    public void Enacting_APendingAllianceTakesASlot()
    {
        Refusal(EdictType.FestivalSeason, level: 1, held: Edicts((EdictType.Alliance, Held(Now, EdictState.Pending, "hutlar"))))
            .Should().Be(EdictRefusal.NoFreeSlot);
    }

    [Test]
    public void Enacting_AnAllianceOrEmbargoMustNameAValidOtherCity()
    {
        Refusal(EdictType.Alliance).Should().Be(EdictRefusal.TargetRequired);
        Refusal(EdictType.Embargo, target: "home").Should().Be(EdictRefusal.InvalidTarget, "a city cannot name itself");
        Refusal(EdictType.Embargo, target: "gone", targetValid: false).Should().Be(EdictRefusal.InvalidTarget);
        Refusal(EdictType.Embargo, target: "hutlar").Should().Be(EdictRefusal.None);
    }

    [Test]
    public void Enacting_AnAllianceAgainstACityTheCityEmbargoesIsRefused()
    {
        var held = Edicts((EdictType.Embargo, Held(target: "hutlar")));

        Refusal(EdictType.Alliance, level: 5, held: held, target: "hutlar").Should().Be(EdictRefusal.ExclusivePair);
        Refusal(EdictType.Alliance, level: 5, held: held, target: "veles").Should().Be(EdictRefusal.None);
    }

    [Test]
    public void EveryRefusal_HasAPlayerFacingReason()
    {
        foreach (var refusal in Enum.GetValues(typeof(EdictRefusal)).Cast<EdictRefusal>().Where(x => x != EdictRefusal.None))
        {
            EdictRules.DescribeRefusal(refusal, EdictType.HuntingGrounds, CityWideCondition.Disrepair)
                .Should().NotBeNullOrWhiteSpace(refusal.ToString())
                .And.MatchRegex("^[\\x20-\\x7E]+$");
        }

        EdictRules.DescribeRefusal(EdictRefusal.LevelTooLow, EdictType.HuntingGrounds).Should().Contain("level 3");
        EdictRules.DescribeRefusal(EdictRefusal.CityWideCondition, EdictType.FestivalSeason, CityWideCondition.NoConfidence)
            .Should().Contain("No confidence");
    }

    #endregion

    #region Level drops

    [Test]
    public void ALevelDrop_PausesTheEdictsWhoseMinimumLevelIsNowTooHigh()
    {
        var edicts = Edicts((EdictType.HuntingGrounds, Held(Now.AddDays(-20))), (EdictType.FestivalSeason, Held(Now.AddDays(-10))));

        // A level 1-2 city has one slot. The minimum-level reason comes first, so the older Edict goes and the newer stays.
        EdictRules.GetCityLevelPauses(edicts, 2).Should().BeEquivalentTo(new[] { EdictType.HuntingGrounds });
    }

    [Test]
    public void ALevelDrop_PausesTheNewestEdictsAboveTheNewSlotCount()
    {
        var edicts = Edicts(
            (EdictType.GuildCharter, Held(Now.AddDays(-30))),
            (EdictType.DrydockSubsidy, Held(Now.AddDays(-20))),
            (EdictType.ArtisansCharter, Held(Now.AddDays(-10))));

        EdictRules.GetCityLevelPauses(edicts, 5).Should().BeEmpty();
        EdictRules.GetCityLevelPauses(edicts, 4).Should().BeEquivalentTo(new[] { EdictType.ArtisansCharter });
        EdictRules.GetCityLevelPauses(edicts, 2).Should().BeEquivalentTo(new[] { EdictType.DrydockSubsidy, EdictType.ArtisansCharter });
    }

    [Test]
    public void ALevelDrop_PausesMinimumLevelReasonsBeforeTheNewest()
    {
        // Level 5 held three; level 3 keeps two slots. Hunting Grounds needs level 3 so it stays allowed, and the newest goes.
        var edicts = Edicts(
            (EdictType.HuntingGrounds, Held(Now.AddDays(-30))),
            (EdictType.FestivalSeason, Held(Now.AddDays(-20))),
            (EdictType.GuildCharter, Held(Now.AddDays(-10))));

        EdictRules.GetCityLevelPauses(edicts, 3).Should().BeEquivalentTo(new[] { EdictType.GuildCharter });

        // At level 2 Hunting Grounds fails its minimum, then one of the other two must also go: the newer.
        EdictRules.GetCityLevelPauses(edicts, 2).Should().BeEquivalentTo(new[] { EdictType.HuntingGrounds, EdictType.GuildCharter });
    }

    [Test]
    public void ALevelDrop_AnEdictPausedForItsMinimumLevelFreesItsSlotForOthers()
    {
        var edicts = Edicts(
            (EdictType.IndustrialCharter, Held(Now.AddDays(-5))),
            (EdictType.FestivalSeason, Held(Now.AddDays(-30))));

        // Two slots at level 3 and 4; Industrial Charter needs 3. At level 2 there is one slot: the minimum-level reason pauses
        // Industrial Charter, which leaves exactly one Edict for the one slot, so the older Edict is not trimmed.
        EdictRules.GetCityLevelPauses(edicts, 2).Should().BeEquivalentTo(new[] { EdictType.IndustrialCharter });
    }

    [Test]
    public void TheLevelReturning_ClearsEveryLevelPause()
    {
        var edicts = Edicts(
            (EdictType.HuntingGrounds, Held(Now.AddDays(-30))),
            (EdictType.GuildCharter, Held(Now.AddDays(-10))));

        EdictRules.GetCityLevelPauses(edicts, 3).Should().BeEmpty();
    }

    [Test]
    public void ALevelDrop_TiesOnEnactedTimeBreakByTheNewerEdictType()
    {
        var same = Now.AddDays(-10);
        var edicts = Edicts((EdictType.GuildCharter, Held(same)), (EdictType.ArtisansCharter, Held(same)));

        EdictRules.GetCityLevelPauses(edicts, 2).Should().BeEquivalentTo(new[] { EdictType.ArtisansCharter });
    }

    #endregion

    #region Citizens

    [Test]
    public void ACitizen_NeedsNoTaxesOwedAndSevenDaysOfCitizenship()
    {
        var since = Now.AddDays(-7);

        EdictRules.IsEligibleCitizen("home", "home", false, 0, since, Now).Should().BeTrue();
        EdictRules.IsEligibleCitizen("home", "home", false, 0, since.AddSeconds(1), Now).Should().BeFalse("not quite 7 days");
        EdictRules.IsEligibleCitizen("home", "home", false, 1, since, Now).Should().BeFalse("taxes are owed");
        EdictRules.IsEligibleCitizen("home", "home", false, 0, null, Now).Should().BeFalse("no citizen-since time");
        EdictRules.IsEligibleCitizen("other", "home", false, 0, since, Now).Should().BeFalse("a citizen of another city");
        EdictRules.IsEligibleCitizen("", "home", false, 0, since, Now).Should().BeFalse("not a citizen");
        EdictRules.IsEligibleCitizen("home", "home", true, 0, since, Now).Should().BeFalse("a deleted character");
    }

    #endregion

    #region Text

    [TestCase(3.5, "3d 12h")]
    [TestCase(1.0, "1d 0h")]
    [TestCase(0.5, "12h 0m")]
    [TestCase(0.01, "15m")]
    [TestCase(0.0, "1m")]
    public void RemainingTime_ReadsAsDaysHoursOrMinutes(double days, string expected)
    {
        EdictRules.FormatRemaining(TimeSpan.FromDays(days)).Should().Be(expected);
    }

    [Test]
    public void HeldText_ShowsNameCostStateLockAndOrigin()
    {
        var edict = Held(Now.AddDays(-3));
        edict.FromPlatform = true;

        EdictRules.BuildHeldText(EdictType.GuildCharter, edict, null, CityWideCondition.None, Now)
            .Should().Be("Guild Charter - 15,000 cr a week\nIn effect - locked for 4d 0h - from the mayor's platform");
    }

    [Test]
    public void HeldText_NamesTheCityAnAllianceNamesAndWhyAnEdictIsPaused()
    {
        var pending = Held(Now, EdictState.Pending, "hutlar");
        EdictRules.BuildHeldText(EdictType.Alliance, pending, "Hutlar Station", CityWideCondition.None, Now)
            .Should().StartWith("Alliance (Hutlar Station) - 5,000 cr a week\nPending");

        EdictRules.BuildHeldText(EdictType.Embargo, Held(target: "gone"), null, CityWideCondition.Disrepair, Now)
            .Should().Be("Embargo (a city that is gone) - 2,500 cr a week\nPaused: city in disrepair - can be repealed");
    }

    [Test]
    public void FundsText_ExplainsRestrictedFundsConditionsAndTheDaysLeft()
    {
        EdictRules.BuildFundsText(120000, 0, CityWideCondition.None, Now, Now.AddDays(3.5))
            .Should().Be("Treasury: 120,000 cr. Enacting an Edict pays at once for the 3.5 days left until the next upkeep check.");

        EdictRules.BuildFundsText(1000, 5000, CityWideCondition.Neglect, Now, Now.AddDays(-1))
            .Should().Contain("plus 5,000 cr that cannot be withdrawn")
            .And.Contain("0.0 days")
            .And.Contain("No Edict can be enacted while the city is under Neglect.");
    }

    [Test]
    public void OfferText_ExplainsWhatAnEdictCostsToEnactNow()
    {
        EdictRules.BuildOfferText(EdictCatalog.Get(EdictType.GuildCharter), 6428)
            .Should().Contain("Costs 15,000 cr a week").And.Contain("Enacting now pays 6,428 cr");
        EdictRules.BuildOfferText(EdictCatalog.Get(EdictType.Alliance), 3000)
            .Should().Contain("costs nothing until the other city also enacts");
    }

    [Test]
    public void GazetteText_NamesTheEdictAndTheCityItNames()
    {
        EdictRules.BuildEnactedText(EdictType.FestivalSeason, null, true).Should().Be("The Festival Season Edict was enacted.");
        EdictRules.BuildEnactedText(EdictType.Embargo, "Hutlar Station", true).Should().Be("The Embargo Edict was enacted, naming Hutlar Station.");
        EdictRules.BuildEnactedText(EdictType.Alliance, "Hutlar Station", false).Should().Contain("takes effect when that city's mayor enacts");
        EdictRules.BuildRepealedText(EdictType.Alliance, "Hutlar Station").Should().Be("The Alliance Edict naming Hutlar Station was repealed.");
        EdictRules.BuildRepealedText(EdictType.GuildCharter, null).Should().Be("The Guild Charter Edict was repealed.");
        EdictRules.BuildAllianceFormedText("Hutlar Station").Should().Be("The Alliance with Hutlar Station took effect.");
        EdictRules.BuildAllianceEndedText("Hutlar Station", "repealed it").Should().Be("The Alliance with Hutlar Station ended because that city repealed it.");
    }

    [Test]
    public void TheOfficialsTitle_IsMayorOrDelegate()
    {
        EdictRules.GetOfficialTitle(true).Should().Be("Mayor");
        EdictRules.GetOfficialTitle(false).Should().Be("Delegate");
    }

    #endregion
}
