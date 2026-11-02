using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The situation definitions: every example in the Plan is defined as written, and every definition stays inside the
/// limits a situation has.
/// </summary>
public class SituationDefinitionTests
{
    private static SituationDetail Get(SituationType type) => SituationCatalog.Get(type);

    private static SituationChoice Choice(SituationType type, string text) =>
        Get(type).Choices.Single(choice => choice.Text == text);

    private static IEnumerable<SituationEffect> Modifiers(IEnumerable<SituationEffect> effects) =>
        effects.Where(effect => effect.Type == SituationEffectType.Modifier);

    private static IEnumerable<SituationEffect> FollowUps(IEnumerable<SituationEffect> effects) =>
        effects.Where(effect => effect.Type == SituationEffectType.FollowUp);

    private static IEnumerable<SituationType> DefinedTypes =>
        Enum.GetValues<SituationType>().Where(type => type != SituationType.Invalid);

    #region Every definition

    [Test]
    public void EverySituationType_HasADefinition()
    {
        foreach (var type in DefinedTypes)
        {
            SituationCatalog.All.Should().ContainKey(type);
        }

        SituationCatalog.All.Keys.Should().BeEquivalentTo(DefinedTypes);
    }

    [Test]
    public void EverySituationHasATitleATextAndTwoOrThreeChoices()
    {
        foreach (var detail in SituationCatalog.All.Values)
        {
            detail.Title.Should().NotBeNullOrWhiteSpace(detail.Type.ToString());
            detail.Text.Should().NotBeNullOrWhiteSpace(detail.Type.ToString());
            detail.Choices.Count.Should().BeInRange(2, 3, detail.Type.ToString());
            detail.Weight.Should().BeGreaterThan(0);
        }
    }

    [Test]
    public void EveryChoiceHasATextAndAGazetteEntry_AndEverySituationAnIgnoredEntry()
    {
        foreach (var detail in SituationCatalog.All.Values)
        {
            detail.IgnoredGazetteText.Should().NotBeNullOrWhiteSpace($"{detail.Type} needs a Gazette entry for being ignored");
            foreach (var choice in detail.Choices)
            {
                choice.Text.Should().NotBeNullOrWhiteSpace();
                choice.GazetteText.Should().NotBeNullOrWhiteSpace($"{detail.Type}: {choice.Text}");
            }
        }
    }

    [Test]
    public void EveryChoiceAndIgnoredOutcome_StaysInsideTheTreasuryLimits()
    {
        foreach (var detail in SituationCatalog.All.Values)
        {
            SituationRules.GetCost(detail.IgnoredEffects).Should().BeLessThanOrEqualTo(SituationRules.MaxCost);
            SituationRules.GetGain(detail.IgnoredEffects).Should().Be(0, $"{detail.Type}: ignoring a situation never pays");
            foreach (var choice in detail.Choices)
            {
                choice.Cost.Should().BeLessThanOrEqualTo(SituationRules.MaxCost, $"{detail.Type}: {choice.Text}");
                SituationRules.GetGain(choice.Effects).Should().BeLessThanOrEqualTo(SituationRules.MaxGain, $"{detail.Type}: {choice.Text}");
                (choice.Cost > 0 && SituationRules.GetGain(choice.Effects) > 0).Should().BeFalse($"{detail.Type}: {choice.Text} both pays and costs");
            }
        }
    }

    [Test]
    public void EveryChoiceThatGainsCredits_CarriesACost()
    {
        foreach (var detail in SituationCatalog.All.Values)
        {
            foreach (var choice in detail.Choices.Where(choice => SituationRules.GetGain(choice.Effects) > 0))
            {
                var hasPenalty = Modifiers(choice.Effects).Any() ||
                                 FollowUps(choice.Effects).Any() ||
                                 choice.IsSecret;
                hasPenalty.Should().BeTrue($"{detail.Type}: {choice.Text} gains credits with no penalty, chance of exposure, or worse follow-up");
            }
        }
    }

    [Test]
    public void EveryFollowUp_PointsAtAFollowUpDefinition()
    {
        foreach (var detail in SituationCatalog.All.Values)
        {
            var effects = detail.Choices.SelectMany(choice => choice.Effects).Concat(detail.IgnoredEffects);
            foreach (var followUp in FollowUps(effects))
            {
                SituationCatalog.All.Should().ContainKey(followUp.FollowUp);
                Get(followUp.FollowUp).IsFollowUp.Should().BeTrue($"{detail.Type} queues {followUp.FollowUp}");
                followUp.FollowUpDelayDays.Should().BeGreaterThan(0);
            }
        }
    }

    [Test]
    public void ASituationDefinedAsAFollowUp_IsQueuedBySomeDecision()
    {
        var queued = SituationCatalog.All.Values
            .SelectMany(detail => detail.Choices.SelectMany(choice => choice.Effects).Concat(detail.IgnoredEffects))
            .Where(effect => effect.Type == SituationEffectType.FollowUp)
            .Select(effect => effect.FollowUp)
            .ToHashSet();

        foreach (var detail in SituationCatalog.All.Values.Where(detail => detail.IsFollowUp))
        {
            queued.Should().Contain(detail.Type);
        }
    }

    [Test]
    public void OnlyAChoiceThatCarriesASecretIsKeptOutOfTheGazette_AndOnlyAScandalExposesIt()
    {
        var secretChoices = SituationCatalog.All.Values
            .SelectMany(detail => detail.Choices.Where(choice => choice.IsSecret).Select(choice => (detail.Type, choice)))
            .ToList();
        secretChoices.Should().ContainSingle().Which.Type.Should().Be(SituationType.SmugglersOffer);

        var carrying = SituationCatalog.All.Values
            .SelectMany(detail => detail.Choices.SelectMany(choice => choice.Effects))
            .Where(effect => effect.Type == SituationEffectType.FollowUp && effect.FollowUpCarriesSecret)
            .ToList();
        carrying.Should().ContainSingle().Which.FollowUp.Should().Be(SituationType.Scandal);

        SituationCatalog.All.Values.Where(detail => detail.ExposesSecret).Select(detail => detail.Type)
            .Should().Equal(SituationType.Scandal);
    }

    [Test]
    public void GameTextIsPlainAscii()
    {
        foreach (var detail in SituationCatalog.All.Values)
        {
            var texts = new List<string> { detail.Title, detail.Text, detail.IgnoredGazetteText };
            foreach (var choice in detail.Choices)
            {
                texts.Add(choice.Text);
                texts.Add(choice.GazetteText);
            }

            foreach (var text in texts)
            {
                text.All(character => character >= ' ' && character <= '~').Should().BeTrue($"{detail.Type}: '{text}'");
            }
        }
    }

    [Test]
    public void FarmSituationsAreLeftForTheFarmingSlices()
    {
        Enum.GetNames<SituationType>().Should().NotContain(name => name.Contains("Pest") || name.Contains("Predator"));
    }

    #endregion

    #region The Plan's examples

    [Test]
    public void DockWorkersStrike_PaysTheRaiseRefusesOrNegotiates()
    {
        var strike = Get(SituationType.DockWorkersStrike);

        Choice(SituationType.DockWorkersStrike, "Pay the raise").Cost.Should().Be(15000);
        Modifiers(Choice(SituationType.DockWorkersStrike, "Refuse").Effects).Single().Modifier
            .Should().Be(CityModifierType.TransportationIncomeHalved);

        var negotiate = Choice(SituationType.DockWorkersStrike, "Negotiate");
        negotiate.Cost.Should().Be(7500);
        negotiate.Requirements.Should().ContainSingle();
        negotiate.Requirements[0].Type.Should().Be(SituationRequirementType.SkillRank);
        negotiate.Requirements[0].Skill.Should().Be(SkillType.Leadership);
        negotiate.Requirements[0].Level.Should().Be(10);

        Modifiers(strike.IgnoredEffects).Single().Modifier.Should().Be(CityModifierType.TransportationIncomeHalved);
    }

    [Test]
    public void FestivalPetition_FundsAFestivalOrDeclines()
    {
        var fund = Choice(SituationType.FestivalPetition, "Fund the festival");
        fund.Cost.Should().Be(10000);
        Modifiers(fund.Effects).Single().Modifier.Should().Be(CityModifierType.FestivalXpBonus);

        var decline = Choice(SituationType.FestivalPetition, "Decline");
        decline.Effects.Should().BeEmpty();
        decline.GazetteText.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void SmugglersOffer_TakesASecretBribeOrArrestsTheSmuggler()
    {
        var bribe = Choice(SituationType.SmugglersOffer, "Take the bribe");
        SituationRules.GetGain(bribe.Effects).Should().Be(10000);
        bribe.IsSecret.Should().BeTrue();
        bribe.IsHook.Should().BeFalse();
        var scandal = FollowUps(bribe.Effects).Single();
        scandal.FollowUp.Should().Be(SituationType.Scandal);
        scandal.FollowUpOneIn.Should().Be(3);
        scandal.FollowUpCarriesSecret.Should().BeTrue();

        var arrest = Choice(SituationType.SmugglersOffer, "Arrest the smuggler");
        arrest.IsHook.Should().BeTrue();
        arrest.IsSecret.Should().BeFalse();
    }

    [Test]
    public void BeastSighting_PostsABountyOrIgnoresItAndAFollowUpArrivesInThreeDays()
    {
        var bounty = Choice(SituationType.BeastSighting, "Post a bounty");
        bounty.Cost.Should().Be(5000);
        bounty.IsHook.Should().BeTrue();

        var ignore = Choice(SituationType.BeastSighting, "Ignore it");
        var followUp = FollowUps(ignore.Effects).Single();
        followUp.FollowUp.Should().Be(SituationType.CaravanAttacked);
        followUp.FollowUpDelayDays.Should().Be(3);
        followUp.FollowUpOneIn.Should().Be(1);

        var ignored = FollowUps(Get(SituationType.BeastSighting).IgnoredEffects).Single();
        ignored.FollowUp.Should().Be(SituationType.CaravanAttacked);
        Get(SituationType.BeastSighting).IgnoredIsHook.Should().BeTrue("an ignored beast sighting goes to the DM feed");
    }

    [Test]
    public void RefugeeShuttle_WelcomesThemWithAFollowUpAndAHookOrTurnsThemAway()
    {
        var welcome = Choice(SituationType.RefugeeShuttle, "Welcome them");
        welcome.Cost.Should().Be(10000);
        welcome.IsHook.Should().BeTrue();
        FollowUps(welcome.Effects).Single().FollowUp.Should().Be(SituationType.NewArrivals);

        var turnAway = Choice(SituationType.RefugeeShuttle, "Turn them away");
        turnAway.Effects.Should().BeEmpty();
        turnAway.GazetteText.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void MedicalSupplyShortage_BuysSuppliesOrRations()
    {
        Choice(SituationType.MedicalSupplyShortage, "Buy supplies").Cost.Should().Be(10000);
        Modifiers(Choice(SituationType.MedicalSupplyShortage, "Ration supplies").Effects).Single().Modifier
            .Should().Be(CityModifierType.MedicalCenterBonusHalved);
    }

    [Test]
    public void VisitorBrawl_NeedsOpenGates_FinesTheVisitorsOrClosesTheGates()
    {
        var brawl = Get(SituationType.VisitorBrawl);
        brawl.Requirements.Should().ContainSingle();
        brawl.Requirements[0].Type.Should().Be(SituationRequirementType.ActiveEdict);
        brawl.Requirements[0].EdictName.Should().Be("OpenGates");

        var fine = Choice(SituationType.VisitorBrawl, "Fine the visitors");
        SituationRules.GetGain(fine.Effects).Should().Be(5000);
        FollowUps(fine.Effects).Single().FollowUp.Should().Be(SituationType.VisitorsComplain);

        Modifiers(Choice(SituationType.VisitorBrawl, "Close the gates").Effects).Single().Modifier
            .Should().Be(CityModifierType.OpenGatesSuspended);
    }

    [Test]
    public void VisitorBrawl_IsOnlyDrawnWhenTheOpenGatesEdictExists()
    {
        var context = new SituationContext { CityLevel = 5 };
        var withEdict = new SituationContext { CityLevel = 5, ActiveEdicts = new List<string> { "OpenGates" } };

        SituationRules.SelectEligible(SituationCatalog.All.Values, context, new HashSet<SituationType>())
            .Select(detail => detail.Type).Should().NotContain(SituationType.VisitorBrawl);
        SituationRules.SelectEligible(SituationCatalog.All.Values, withEdict, new HashSet<SituationType>())
            .Select(detail => detail.Type).Should().Contain(SituationType.VisitorBrawl);
    }

    [Test]
    public void Scandal_ExposesTheBribeWhenItArrives()
    {
        var scandal = Get(SituationType.Scandal);

        scandal.ExposesSecret.Should().BeTrue();
        scandal.IsFollowUp.Should().BeTrue();
        scandal.Choices.Should().OnlyContain(choice => choice.IsHook);
        scandal.IgnoredIsHook.Should().BeTrue();
    }

    #endregion

    #region Fitting the city

    private static List<SituationType> Drawable(SituationContext context) =>
        SituationRules.SelectEligible(SituationCatalog.All.Values, context, new HashSet<SituationType>())
            .Select(detail => detail.Type)
            .ToList();

    [Test]
    public void ABareLevelOneOutpost_DrawsOnlyFestivalAndBeastSightings()
    {
        Drawable(new SituationContext { CityLevel = 1 })
            .Should().BeEquivalentTo(new[] { SituationType.FestivalPetition, SituationType.BeastSighting });
    }

    [Test]
    public void AStation_NeverDrawsABeastSighting()
    {
        Drawable(new SituationContext { CityLevel = 5, IsStation = true })
            .Should().NotContain(SituationType.BeastSighting);
    }

    [Test]
    public void AStarportCity_DrawsStrikesSmugglersAndRefugees()
    {
        var context = new SituationContext
        {
            CityLevel = 2,
            FacilityLevels = new Dictionary<PropertyUpgradeType, int> { { PropertyUpgradeType.StarportLevel, 1 } }
        };

        Drawable(context).Should().Contain(new[]
        {
            SituationType.DockWorkersStrike, SituationType.SmugglersOffer, SituationType.RefugeeShuttle
        });
        Drawable(context).Should().NotContain(SituationType.MedicalSupplyShortage);
    }

    [Test]
    public void AFollowUp_IsNeverDrawnAtRandom()
    {
        var context = new SituationContext
        {
            CityLevel = 5,
            FacilityLevels = new Dictionary<PropertyUpgradeType, int>
            {
                { PropertyUpgradeType.StarportLevel, 3 }, { PropertyUpgradeType.MedicalCenterLevel, 3 }
            },
            ActiveEdicts = new List<string> { "OpenGates" }
        };

        var drawable = Drawable(context);

        drawable.Should().NotContain(new[]
        {
            SituationType.CaravanAttacked, SituationType.NewArrivals, SituationType.VisitorsComplain, SituationType.Scandal
        });
        drawable.Should().HaveCount(7);
    }

    #endregion
}
