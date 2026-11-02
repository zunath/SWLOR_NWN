using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The City Affairs situation rules: what can be drawn and when, how a choice is checked and paid for, the treasury
/// limits, follow-ups, modifiers, and the Leadership XP reward.
/// </summary>
public class SituationRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static CitySituation Situation(
        SituationType type,
        CitySituationState state,
        DateTime? drawn = null,
        DateTime? resolved = null,
        int gain = 0,
        DateTime? due = null)
    {
        var drawnUtc = drawn ?? Now.AddHours(-1);
        return new CitySituation
        {
            CityId = "city",
            Type = type,
            State = state,
            DrawnUtc = state == CitySituationState.Queued ? default : drawnUtc,
            ExpiresUtc = drawnUtc.AddHours(SituationRules.ExpiryHours),
            DueUtc = due ?? default,
            ResolvedUtc = resolved,
            TreasuryGain = gain
        };
    }

    private static SituationContext Context(
        int level = 3,
        bool isStation = false,
        PlanetType planet = PlanetType.Viscara,
        IEnumerable<PropertyUpgradeType> facilities = null,
        IEnumerable<string> edicts = null,
        IDictionary<SkillType, int> skills = null)
    {
        return new SituationContext
        {
            CityLevel = level,
            IsStation = isStation,
            Planet = planet,
            FacilityLevels = (facilities ?? Enumerable.Empty<PropertyUpgradeType>()).ToDictionary(type => type, _ => 1),
            ActiveEdicts = (edicts ?? Enumerable.Empty<string>()).ToList(),
            SkillRanks = skills == null ? new Dictionary<SkillType, int>() : new Dictionary<SkillType, int>(skills)
        };
    }

    #region Treasury limits

    [TestCase(-20000, 100000L, -15000)]
    [TestCase(-15000, 100000L, -15000)]
    [TestCase(-7500, 100000L, -7500)]
    [TestCase(0, 100000L, 0)]
    [TestCase(5000, 100000L, 5000)]
    [TestCase(10000, 100000L, 10000)]
    [TestCase(50000, 100000L, 10000)]
    public void ATreasuryChange_IsClampedToACostOf15000AndAGainOf10000(int requested, long treasury, int expected)
    {
        SituationRules.ResolveTreasuryChange(requested, treasury, 0).Should().Be(expected);
    }

    [TestCase(15000, 9000L, -9000)]
    [TestCase(15000, 0L, 0)]
    [TestCase(5000, 5000L, -5000)]
    [TestCase(5000, -300L, 0)]
    public void ACost_NeverTakesMoreThanTheTreasuryHolds(int cost, long treasury, int expected)
    {
        SituationRules.ResolveTreasuryChange(-cost, treasury, 0).Should().Be(expected);
    }

    [TestCase(0, 10000)]
    [TestCase(10000, 10000)]
    [TestCase(15000, 5000)]
    [TestCase(19999, 1)]
    [TestCase(20000, 0)]
    [TestCase(25000, 0)]
    public void Gains_StopAtTheWeeklyCapOf20000(int gainedThisWeek, int expectedGain)
    {
        SituationRules.ResolveTreasuryChange(10000, 0, gainedThisWeek).Should().Be(expectedGain);
    }

    [Test]
    public void TheWeeklyCap_DoesNotLimitACost()
    {
        SituationRules.ResolveTreasuryChange(-5000, 100000, 20000).Should().Be(-5000);
    }

    [Test]
    public void GainsInTheWindow_CountOnlyTheLastSevenDays()
    {
        var situations = new List<CitySituation>
        {
            Situation(SituationType.SmugglersOffer, CitySituationState.Decided, resolved: Now.AddDays(-1), gain: 10000),
            Situation(SituationType.VisitorBrawl, CitySituationState.Decided, resolved: Now.AddDays(-6), gain: 5000),
            Situation(SituationType.SmugglersOffer, CitySituationState.Decided, resolved: Now.AddDays(-8), gain: 10000),
            Situation(SituationType.FestivalPetition, CitySituationState.Decided, resolved: Now.AddHours(-2))
        };

        SituationRules.GetGainsInWindow(situations, Now).Should().Be(15000);
    }

    [Test]
    public void GainsInTheWindow_RollOffAsTheWeekMovesOn()
    {
        var situations = new List<CitySituation>
        {
            Situation(SituationType.SmugglersOffer, CitySituationState.Decided, resolved: Now.AddDays(-6).AddHours(-23), gain: 10000)
        };

        SituationRules.GetGainsInWindow(situations, Now).Should().Be(10000);
        SituationRules.GetGainsInWindow(situations, Now.AddHours(2)).Should().Be(0);
    }

    [Test]
    public void ARepeatedGain_UsesTheRemainingCapAcrossSituations()
    {
        var first = SituationRules.ResolveTreasuryChange(10000, 0, 0);
        var second = SituationRules.ResolveTreasuryChange(10000, 0, first);
        var third = SituationRules.ResolveTreasuryChange(10000, 0, first + second);

        (first + second + third).Should().Be(SituationRules.WeeklyGainCap);
        third.Should().Be(0);
    }

    [Test]
    public void AChoice_IsDisabledWhenTheTreasuryCannotAffordIt()
    {
        SituationRules.CanAfford(15000, 14999).Should().BeFalse();
        SituationRules.CanAfford(15000, 15000).Should().BeTrue();
        SituationRules.CanAfford(0, 0).Should().BeTrue();
    }

    [Test]
    public void ChoiceCost_IsWhatItsEffectsTakeFromTheTreasury()
    {
        var effects = new[]
        {
            SituationEffect.TreasuryChange(-10000),
            SituationEffect.AddModifier(CityModifierType.FestivalXpBonus)
        };
        var gain = new[] { SituationEffect.TreasuryChange(10000) };

        SituationRules.GetCost(effects).Should().Be(10000);
        SituationRules.GetCost(gain).Should().Be(0);
        SituationRules.GetGain(gain).Should().Be(10000);
        SituationRules.GetRequestedChange(effects).Should().Be(-10000);
    }

    #endregion

    #region Requirements

    [Test]
    public void CityLevelRequirement_NeedsThatLevelOrHigher()
    {
        var requirement = SituationRequirement.CityLevel(3);

        SituationRules.IsMet(requirement, Context(level: 2)).Should().BeFalse();
        SituationRules.IsMet(requirement, Context(level: 3)).Should().BeTrue();
        SituationRules.IsMet(requirement, Context(level: 5)).Should().BeTrue();
    }

    [Test]
    public void PlanetRequirement_AcceptsAnyListedPlanet()
    {
        var requirement = SituationRequirement.OnPlanet(PlanetType.Viscara | PlanetType.Tatooine);

        SituationRules.IsMet(requirement, Context(planet: PlanetType.Tatooine)).Should().BeTrue();
        SituationRules.IsMet(requirement, Context(planet: PlanetType.MonCala)).Should().BeFalse();
        SituationRules.IsMet(requirement, Context(planet: PlanetType.Invalid)).Should().BeFalse();
    }

    [Test]
    public void PlanetCityRequirement_ExcludesStations()
    {
        SituationRules.IsMet(SituationRequirement.PlanetCity(), Context(isStation: false)).Should().BeTrue();
        SituationRules.IsMet(SituationRequirement.PlanetCity(), Context(isStation: true)).Should().BeFalse();
    }

    [Test]
    public void FacilityRequirement_NeedsTheFacilityAtThatLevel()
    {
        var requirement = SituationRequirement.WithFacility(PropertyUpgradeType.StarportLevel, 1);

        SituationRules.IsMet(requirement, Context()).Should().BeFalse();
        SituationRules.IsMet(requirement, Context(facilities: new[] { PropertyUpgradeType.MedicalCenterLevel })).Should().BeFalse();
        SituationRules.IsMet(requirement, Context(facilities: new[] { PropertyUpgradeType.StarportLevel })).Should().BeTrue();
        SituationRules.IsMet(SituationRequirement.WithFacility(PropertyUpgradeType.StarportLevel, 2),
            Context(facilities: new[] { PropertyUpgradeType.StarportLevel })).Should().BeFalse();
    }

    [Test]
    public void EdictRequirement_NeedsTheEdictInForce()
    {
        var requirement = SituationRequirement.ActiveEdict("OpenGates");

        SituationRules.IsMet(requirement, Context()).Should().BeFalse();
        SituationRules.IsMet(requirement, Context(edicts: new[] { "FestivalSeason" })).Should().BeFalse();
        SituationRules.IsMet(requirement, Context(edicts: new[] { "OpenGates" })).Should().BeTrue();
    }

    [Test]
    public void SkillRequirement_NeedsTheRank()
    {
        var requirement = SituationRequirement.SkillRank(SkillType.Leadership, 10);

        SituationRules.IsMet(requirement, Context()).Should().BeFalse();
        SituationRules.IsMet(requirement, Context(skills: new Dictionary<SkillType, int> { { SkillType.Leadership, 9 } })).Should().BeFalse();
        SituationRules.IsMet(requirement, Context(skills: new Dictionary<SkillType, int> { { SkillType.Leadership, 10 } })).Should().BeTrue();
        SituationRules.IsMet(requirement, Context(skills: new Dictionary<SkillType, int> { { SkillType.Gathering, 40 } })).Should().BeFalse();
    }

    [Test]
    public void UnmetRequirements_AreDescribedForThePlayer()
    {
        var requirements = new[]
        {
            SituationRequirement.CityLevel(2),
            SituationRequirement.WithFacility(PropertyUpgradeType.MedicalCenterLevel),
            SituationRequirement.ActiveEdict("OpenGates"),
            SituationRequirement.SkillRank(SkillType.Leadership, 10)
        };

        SituationRules.DescribeUnmet(requirements, Context(level: 1))
            .Should().Equal("City level 2", "Medical Center level 1", "Open Gates in force", "Leadership 10");
        SituationRules.DescribeUnmet(requirements, Context(
                level: 2,
                facilities: new[] { PropertyUpgradeType.MedicalCenterLevel },
                edicts: new[] { "OpenGates" },
                skills: new Dictionary<SkillType, int> { { SkillType.Leadership, 12 } }))
            .Should().BeEmpty();
    }

    #endregion

    #region Drawing

    [Test]
    public void ACityDraws_WhenFewerThanThreeAreOpenAndNoneWasDrawnToday()
    {
        SituationRules.CanDrawNew(0, null, false, false, Now).Should().BeTrue();
        SituationRules.CanDrawNew(2, Now.AddHours(-25), false, false, Now).Should().BeTrue();
        SituationRules.CanDrawNew(3, Now.AddHours(-25), false, false, Now).Should().BeFalse();
        SituationRules.CanDrawNew(5, null, false, false, Now).Should().BeFalse();
    }

    [Test]
    public void ACityDrawsAtMostOneSituationEveryTwentyFourHours()
    {
        SituationRules.CanDrawNew(0, Now.AddHours(-23), false, false, Now).Should().BeFalse();
        SituationRules.CanDrawNew(0, Now.AddHours(-24), false, false, Now).Should().BeTrue();
        SituationRules.CanDrawNew(0, Now.AddMinutes(-5), false, false, Now).Should().BeFalse();
    }

    [Test]
    public void ACityDrawsNothing_WhileInDisrepairOrPausedForNeglect()
    {
        SituationRules.CanDrawNew(0, null, true, false, Now).Should().BeFalse();
        SituationRules.CanDrawNew(0, null, false, true, Now).Should().BeFalse();
        SituationRules.CanArriveFollowUp(0, true, false).Should().BeFalse();
        SituationRules.CanArriveFollowUp(0, false, true).Should().BeFalse();
    }

    [Test]
    public void AFollowUp_ArrivesOnceFewerThanThreeAreOpen_WhateverTheDailyLimit()
    {
        SituationRules.CanArriveFollowUp(2, false, false).Should().BeTrue();
        SituationRules.CanArriveFollowUp(3, false, false).Should().BeFalse();
    }

    [Test]
    public void ACityIsInDisrepair_OnlyWhileItOwesUpkeepPastItsDeadline()
    {
        SituationRules.IsInDisrepair(35000, true).Should().BeTrue();
        SituationRules.IsInDisrepair(0, true).Should().BeFalse("paying the upkeep ends disrepair at once");
        SituationRules.IsInDisrepair(35000, false).Should().BeFalse("a bill inside its first week is not disrepair yet");
    }

    [Test]
    public void LastDrawn_IgnoresFollowUpsStillWaiting()
    {
        var situations = new List<CitySituation>
        {
            Situation(SituationType.FestivalPetition, CitySituationState.Decided, drawn: Now.AddDays(-3)),
            Situation(SituationType.BeastSighting, CitySituationState.Expired, drawn: Now.AddDays(-1)),
            Situation(SituationType.CaravanAttacked, CitySituationState.Queued, due: Now.AddDays(1))
        };

        SituationRules.GetLastDrawnUtc(situations).Should().Be(Now.AddDays(-1));
        SituationRules.GetLastDrawnUtc(new List<CitySituation>()).Should().BeNull();
    }

    [Test]
    public void OnlyOpenSituationsCountAsOpen()
    {
        var situations = new List<CitySituation>
        {
            Situation(SituationType.FestivalPetition, CitySituationState.Open),
            Situation(SituationType.BeastSighting, CitySituationState.Decided),
            Situation(SituationType.NewArrivals, CitySituationState.Queued, due: Now),
            Situation(SituationType.MedicalSupplyShortage, CitySituationState.Open)
        };

        SituationRules.CountOpen(situations).Should().Be(2);
        SituationRules.GetOpenTypes(situations).Should().BeEquivalentTo(
            new[] { SituationType.FestivalPetition, SituationType.MedicalSupplyShortage });
    }

    [Test]
    public void EligibleDefinitions_LeaveOutFollowUpsOpenTypesAndUnmetRequirements()
    {
        var festival = Detail(SituationType.FestivalPetition);
        var strike = Detail(SituationType.DockWorkersStrike);
        strike.Requirements.Add(SituationRequirement.WithFacility(PropertyUpgradeType.StarportLevel));
        var caravan = Detail(SituationType.CaravanAttacked);
        caravan.IsFollowUp = true;
        var beast = Detail(SituationType.BeastSighting);
        var all = new[] { festival, strike, caravan, beast };

        SituationRules.SelectEligible(all, Context(), new HashSet<SituationType>())
            .Should().BeEquivalentTo(new[] { festival, beast });
        SituationRules.SelectEligible(all, Context(facilities: new[] { PropertyUpgradeType.StarportLevel }), new HashSet<SituationType>())
            .Should().BeEquivalentTo(new[] { festival, strike, beast });
        SituationRules.SelectEligible(all, Context(), new HashSet<SituationType> { SituationType.BeastSighting })
            .Should().BeEquivalentTo(new[] { festival });
    }

    [Test]
    public void EligibleDefinitions_NeedAChoiceAndAWeight()
    {
        var withoutChoices = new SituationDetail(SituationType.FestivalPetition);
        var withoutWeight = Detail(SituationType.BeastSighting);
        withoutWeight.Weight = 0;

        SituationRules.SelectEligible(new[] { withoutChoices, withoutWeight }, Context(), new HashSet<SituationType>())
            .Should().BeEmpty();
    }

    [Test]
    public void ARandomPick_FollowsTheWeights()
    {
        var light = Detail(SituationType.FestivalPetition, 10);
        var heavy = Detail(SituationType.BeastSighting, 30);
        var list = new List<SituationDetail> { light, heavy };

        SituationRules.PickWeighted(list, 0.0).Should().BeSameAs(light);
        SituationRules.PickWeighted(list, 0.24).Should().BeSameAs(light);
        SituationRules.PickWeighted(list, 0.25).Should().BeSameAs(heavy);
        SituationRules.PickWeighted(list, 0.9999).Should().BeSameAs(heavy);
        SituationRules.PickWeighted(new List<SituationDetail>(), 0.5).Should().BeNull();
    }

    private static SituationDetail Detail(SituationType type, int weight = SituationRules.DefaultWeight)
    {
        var detail = new SituationDetail(type) { Weight = weight };
        detail.Choices.Add(new SituationChoice("Choice"));
        return detail;
    }

    #endregion

    #region Life of a situation

    [Test]
    public void ASituation_ExpiresSeventyTwoHoursAfterItIsDrawn()
    {
        SituationRules.GetExpiryUtc(Now).Should().Be(Now.AddHours(72));
    }

    [Test]
    public void AnOpenSituation_IsOverdueOnceItsTimeRunsOut_AndCannotBeDecided()
    {
        var situation = Situation(SituationType.FestivalPetition, CitySituationState.Open, drawn: Now.AddHours(-71));

        SituationRules.IsOverdue(situation, Now).Should().BeFalse();
        SituationRules.CanDecide(situation, Now).Should().BeTrue();

        SituationRules.IsOverdue(situation, Now.AddHours(1)).Should().BeTrue("it ran out of time exactly 72 hours after it was drawn");
        SituationRules.CanDecide(situation, Now.AddHours(1)).Should().BeFalse();
    }

    [Test]
    public void ASettledSituation_IsNeitherOverdueNorDecidable()
    {
        var decided = Situation(SituationType.FestivalPetition, CitySituationState.Decided, drawn: Now.AddDays(-5));
        var expired = Situation(SituationType.BeastSighting, CitySituationState.Expired, drawn: Now.AddDays(-5));

        SituationRules.IsOverdue(decided, Now).Should().BeFalse();
        SituationRules.IsOverdue(expired, Now).Should().BeFalse();
        SituationRules.CanDecide(decided, Now).Should().BeFalse();
        SituationRules.CanDecide(null, Now).Should().BeFalse();
    }

    [Test]
    public void FollowUps_ArriveAtTheFirstStartAfterTheyAreDue_OldestFirst()
    {
        var later = Situation(SituationType.NewArrivals, CitySituationState.Queued, due: Now.AddHours(-2));
        var earlier = Situation(SituationType.CaravanAttacked, CitySituationState.Queued, due: Now.AddDays(-2));
        var notYet = Situation(SituationType.VisitorsComplain, CitySituationState.Queued, due: Now.AddMinutes(1));

        SituationRules.SelectDueFollowUps(new[] { later, notYet, earlier }, new HashSet<SituationType>(), Now)
            .Should().Equal(earlier, later);
    }

    [Test]
    public void AFollowUp_WaitsWhileOneOfItsTypeIsOpen()
    {
        var followUp = Situation(SituationType.CaravanAttacked, CitySituationState.Queued, due: Now.AddDays(-1));

        SituationRules.SelectDueFollowUps(new[] { followUp }, new HashSet<SituationType> { SituationType.CaravanAttacked }, Now)
            .Should().BeEmpty();
    }

    [Test]
    public void AFollowUp_IsNotQueuedTwice()
    {
        var situations = new List<CitySituation>
        {
            Situation(SituationType.CaravanAttacked, CitySituationState.Queued, due: Now),
            Situation(SituationType.NewArrivals, CitySituationState.Open),
            Situation(SituationType.VisitorsComplain, CitySituationState.Decided)
        };

        SituationRules.CanQueueFollowUp(SituationType.CaravanAttacked, situations).Should().BeFalse();
        SituationRules.CanQueueFollowUp(SituationType.NewArrivals, situations).Should().BeFalse();
        SituationRules.CanQueueFollowUp(SituationType.VisitorsComplain, situations).Should().BeTrue();
        SituationRules.CanQueueFollowUp(SituationType.Scandal, situations).Should().BeTrue();
    }

    [TestCase(1, 0, true)]
    [TestCase(3, 0, true)]
    [TestCase(3, 1, false)]
    [TestCase(3, 2, false)]
    public void AOneInThreeFollowUp_IsQueuedForOneRollInThree(int oneIn, int roll, bool queued)
    {
        SituationRules.RollsFollowUp(oneIn, roll).Should().Be(queued);
    }

    [Test]
    public void FinishedSituations_AreKeptForTwoWeeks()
    {
        SituationRules.ShouldPrune(
            Situation(SituationType.FestivalPetition, CitySituationState.Decided, resolved: Now.AddDays(-13)), Now).Should().BeFalse();
        SituationRules.ShouldPrune(
            Situation(SituationType.FestivalPetition, CitySituationState.Expired, resolved: Now.AddDays(-15)), Now).Should().BeTrue();
        SituationRules.ShouldPrune(
            Situation(SituationType.FestivalPetition, CitySituationState.Open, drawn: Now.AddDays(-20)), Now).Should().BeFalse();
        SituationRules.ShouldPrune(
            Situation(SituationType.CaravanAttacked, CitySituationState.Queued, due: Now.AddDays(-20)), Now).Should().BeFalse();
    }

    #endregion

    #region Modifiers

    [Test]
    public void AModifier_LastsSeventyTwoHours()
    {
        var modifiers = new Dictionary<CityModifierType, CityModifier>();

        SituationRules.RefreshModifier(modifiers, CityModifierType.FestivalXpBonus, SituationType.FestivalPetition, Now);

        modifiers[CityModifierType.FestivalXpBonus].ExpiresUtc.Should().Be(Now.AddHours(72));
        modifiers[CityModifierType.FestivalXpBonus].Source.Should().Be(SituationType.FestivalPetition);
        SituationRules.IsModifierActive(modifiers, CityModifierType.FestivalXpBonus, Now.AddHours(71)).Should().BeTrue();
        SituationRules.IsModifierActive(modifiers, CityModifierType.FestivalXpBonus, Now.AddHours(72)).Should().BeFalse();
        SituationRules.IsModifierActive(modifiers, CityModifierType.MedicalCenterBonusHalved, Now).Should().BeFalse();
    }

    [Test]
    public void ARepeatedModifier_RestartsItsDurationInsteadOfStacking()
    {
        var modifiers = new Dictionary<CityModifierType, CityModifier>();
        SituationRules.RefreshModifier(modifiers, CityModifierType.TransportationIncomeHalved, SituationType.DockWorkersStrike, Now);

        SituationRules.RefreshModifier(modifiers, CityModifierType.TransportationIncomeHalved, SituationType.CaravanAttacked, Now.AddHours(40));

        modifiers.Should().HaveCount(1);
        modifiers[CityModifierType.TransportationIncomeHalved].ExpiresUtc.Should().Be(Now.AddHours(40 + 72));
        modifiers[CityModifierType.TransportationIncomeHalved].Source.Should().Be(SituationType.CaravanAttacked);
    }

    [Test]
    public void ExpiredModifiers_ArePruned()
    {
        var modifiers = new Dictionary<CityModifierType, CityModifier>();
        SituationRules.RefreshModifier(modifiers, CityModifierType.FestivalXpBonus, SituationType.FestivalPetition, Now.AddHours(-80));
        SituationRules.RefreshModifier(modifiers, CityModifierType.OpenGatesSuspended, SituationType.VisitorBrawl, Now);

        SituationRules.PruneModifiers(modifiers, Now).Should().BeTrue();

        modifiers.Keys.Should().Equal(CityModifierType.OpenGatesSuspended);
        SituationRules.PruneModifiers(modifiers, Now).Should().BeFalse();
    }

    #endregion

    #region Reward

    [Test]
    public void ADecision_IsAtTenTimesTheCityLevel()
    {
        Enumerable.Range(1, 5).Select(SituationRules.GetActivityLevel).Should().Equal(10, 20, 30, 40, 50);
        SituationRules.GetActivityLevel(0).Should().Be(10);
    }

    [TestCase(600, 150)]
    [TestCase(1200, 300)]
    [TestCase(76, 19)]
    [TestCase(0, 0)]
    public void ALeadershipDecision_PaysAQuarterOfTheChartXp(int chartXp, int expected)
    {
        SituationRules.GetLeadershipXp(chartXp).Should().Be(expected);
    }

    #endregion

    #region Display

    [TestCase(0, 0, "Out of time")]
    [TestCase(1, 30, "1m left")]
    [TestCase(90, 0, "1h 30m left")]
    public void TimeLeft_ReadsInTheLargestUnits(int minutes, int extraSeconds, string expected)
    {
        SituationRules.FormatTimeLeft(Now.AddMinutes(minutes).AddSeconds(extraSeconds), Now).Should().Be(expected);
    }

    [Test]
    public void TimeLeft_ShowsDaysAndHours()
    {
        SituationRules.FormatTimeLeft(Now.AddHours(52), Now).Should().Be("2d 4h left");
    }

    #endregion
}
