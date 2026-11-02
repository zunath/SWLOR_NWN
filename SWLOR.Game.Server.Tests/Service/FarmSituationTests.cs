using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The two farm situations of City Affairs, "Pests in the gardens" and "Predators at the corrals", and what the pests do to a
/// harvest.
/// </summary>
public class FarmSituationTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static SituationDetail Pests => SituationCatalog.Get(SituationType.PestsInTheGardens);

    private static SituationDetail Predators => SituationCatalog.Get(SituationType.PredatorsAtTheCorrals);

    private static SituationChoice Choice(SituationDetail detail, string text) => detail.Choices.Single(choice => choice.Text == text);

    private static SituationContext City(params StructureType[] structures) => new()
    {
        CityLevel = 3,
        IsStation = false,
        StructureTypes = structures,
    };

    // Pests in the gardens

    [Test]
    public void Pests_HireExterminatorsForFiveThousandOrIgnoreIt()
    {
        Pests.Title.Should().Be("Pests in the Gardens");
        Pests.Choices.Select(choice => choice.Text).Should().Equal("Hire exterminators", "Ignore it");

        var hire = Choice(Pests, "Hire exterminators");
        hire.Cost.Should().Be(5000);
        hire.Effects.Should().ContainSingle().Which.Type.Should().Be(SituationEffectType.TreasuryChange);

        var ignore = Choice(Pests, "Ignore it");
        ignore.Cost.Should().Be(0);
        ignore.Effects.Should().ContainSingle().Which.Modifier.Should().Be(CityModifierType.CropPests);
    }

    [Test]
    public void Pests_LeftToExpireTakeTheSameOutcomeAsIgnoringThem()
    {
        Pests.IgnoredEffects.Should().ContainSingle().Which.Modifier.Should().Be(CityModifierType.CropPests);
        SituationRules.GetCost(Pests.IgnoredEffects).Should().Be(0);
        Pests.IgnoredGazetteText.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void Pests_NeedAGardenPlotInTheCity_AndNothingPermanentHappens()
    {
        Pests.Requirements.Should().ContainSingle().Which.Structure.Should().Be(StructureType.GardenPlot);
        SituationRules.AreMet(Pests.Requirements, City(StructureType.GardenPlot)).Should().BeTrue();
        SituationRules.AreMet(Pests.Requirements, City(StructureType.Corral)).Should().BeFalse();
        SituationRules.AreMet(Pests.Requirements, City()).Should().BeFalse();
        Pests.IsFollowUp.Should().BeFalse();
        Pests.Text.Should().Contain("Crops keep growing");
    }

    [Test]
    public void Pests_AreOnTheModifierThatHarvestsRead_ForSeventyTwoHours()
    {
        var modifiers = new Dictionary<CityModifierType, CityModifier>();

        SituationRules.RefreshModifier(modifiers, CityModifierType.CropPests, SituationType.PestsInTheGardens, Now);

        SituationRules.IsModifierActive(modifiers, CityModifierType.CropPests, Now.AddHours(71)).Should().BeTrue();
        SituationRules.IsModifierActive(modifiers, CityModifierType.CropPests, Now.AddHours(72)).Should().BeFalse();
        SituationRules.DescribeModifier(CityModifierType.CropPests).Should().Be("crops harvested in the city yield 1 less");

        // A repeat restarts the 72 hours instead of stacking.
        SituationRules.RefreshModifier(modifiers, CityModifierType.CropPests, SituationType.PestsInTheGardens, Now.AddHours(40));
        modifiers.Should().ContainSingle();
        SituationRules.IsModifierActive(modifiers, CityModifierType.CropPests, Now.AddHours(100)).Should().BeTrue();
    }

    [Test]
    public void PestsTakeOneFromAHarvest_NeverBelowOne()
    {
        FarmingRules.PestYieldPenalty.Should().Be(1);
        FarmingRules.GetYield(false, false, 0, false, false).Should().Be(3);
        FarmingRules.GetYield(false, false, 0, false, false, pests: true).Should().Be(2);
        FarmingRules.GetYield(true, true, 3, true, true).Should().Be(10);
        FarmingRules.GetYield(true, true, 3, true, true, pests: true).Should().Be(9);
        FarmingRules.GetYield(false, false, 0, false, false, pests: false).Should().Be(FarmingRules.GetYield(false, false, 0, false, false));

        foreach (var tended in new[] { false, true })
        foreach (var fertilized in new[] { false, true })
        foreach (var rank in Enumerable.Range(0, 4))
            FarmingRules.GetYield(tended, fertilized, rank, false, false, pests: true).Should().BeGreaterThanOrEqualTo(1);
    }

    // Predators at the corrals

    [Test]
    public void Predators_CallAHuntOrIgnoreIt()
    {
        Predators.Title.Should().Be("Predators at the Corrals");
        Predators.Choices.Select(choice => choice.Text).Should().Equal("Call a hunt", "Ignore it");

        var hunt = Choice(Predators, "Call a hunt");
        hunt.Cost.Should().Be(0, "a hunt costs the treasury nothing");
        hunt.Effects.Should().ContainSingle().Which.Type.Should().Be(SituationEffectType.StartHunt);

        var ignore = Choice(Predators, "Ignore it");
        ignore.Effects.Should().ContainSingle().Which.Type.Should().Be(SituationEffectType.LivestockNoProducts);
    }

    [Test]
    public void Predators_LeftToExpireLeaveTheAnimalsWithoutProducts()
    {
        Predators.IgnoredEffects.Should().ContainSingle().Which.Type.Should().Be(SituationEffectType.LivestockNoProducts);
        SituationRules.GetCost(Predators.IgnoredEffects).Should().Be(0);
        Predators.IgnoredGazetteText.Should().Contain("72 hours");
    }

    [Test]
    public void Predators_NeedAPlanetCityWithACorral()
    {
        Predators.Requirements.Select(requirement => requirement.Type)
            .Should().BeEquivalentTo(new[] { SituationRequirementType.PlanetCity, SituationRequirementType.Structure });
        SituationRules.AreMet(Predators.Requirements, City(StructureType.Corral)).Should().BeTrue();
        SituationRules.AreMet(Predators.Requirements, City(StructureType.GardenPlot)).Should().BeFalse();
        SituationRules.AreMet(Predators.Requirements, new SituationContext { IsStation = true, StructureTypes = new[] { StructureType.Corral } })
            .Should().BeFalse("a station deck has no corrals");
    }

    [Test]
    public void TheHuntAndThePenaltyAreDescribedForTheOfficialsWhoChoose()
    {
        var hunt = SituationRules.DescribeEffects(Choice(Predators, "Call a hunt").Effects);
        hunt.Should().Contain("30 minutes").And.Contain("72 hours");
        SituationRules.DescribeEffects(Choice(Predators, "Ignore it").Effects).Should().Contain("72 hours");
        SituationRules.DescribeEffects(Choice(Pests, "Ignore it").Effects).Should().Contain("crops harvested in the city yield 1 less");
        SituationRules.DescribeEffects(Choice(Pests, "Hire exterminators").Effects).Should().StartWith("Costs");
    }

    [Test]
    public void ARequirementThatNamesAStructureReadsAsPlainText()
    {
        SituationRules.Describe(SituationRequirement.WithStructure(StructureType.GardenPlot)).Should().Be("A Garden Plot in the city");
        SituationRules.Describe(SituationRequirement.WithStructure(StructureType.Corral)).Should().Be("A Corral in the city");
    }

    // Drawing

    [Test]
    public void ACityWithNeitherGardensNorCorralsNeverDrawsEither()
    {
        var eligible = SituationRules.SelectEligible(SituationCatalog.All.Values, City(), new HashSet<SituationType>());

        eligible.Select(detail => detail.Type).Should().NotContain(new[] { SituationType.PestsInTheGardens, SituationType.PredatorsAtTheCorrals });
    }

    [Test]
    public void ACityWithBothMayDrawEither_ButOneOfAKindAtATime()
    {
        var eligible = SituationRules.SelectEligible(
            SituationCatalog.All.Values,
            City(StructureType.GardenPlot, StructureType.Corral),
            new HashSet<SituationType> { SituationType.PestsInTheGardens });

        eligible.Select(detail => detail.Type).Should().Contain(SituationType.PredatorsAtTheCorrals).And.NotContain(SituationType.PestsInTheGardens);
    }

    [Test]
    public void NeitherSituationIsAFollowUp_AndBothHaveTheDefaultWeight()
    {
        foreach (var detail in new[] { Pests, Predators })
        {
            detail.IsFollowUp.Should().BeFalse();
            detail.Weight.Should().Be(SituationRules.DefaultWeight);
        }
    }

    [Test]
    public void TheFarmSituationTextIsPlainAscii()
    {
        foreach (var detail in new[] { Pests, Predators })
        {
            var texts = new List<string> { detail.Title, detail.Text, detail.IgnoredGazetteText };
            foreach (var choice in detail.Choices)
            {
                texts.Add(choice.Text);
                texts.Add(choice.GazetteText);
            }

            texts.Should().OnlyContain(text => text.All(character => character >= ' ' && character <= '~'));
        }
    }

    [Test]
    public void ThePenaltyTheSituationsApplyMatchesTheRulesTheAnimalsFollow()
    {
        // The ignored outcome and a lost hunt both put the 72-hour penalty on every animal (LivestockRules.PenaltyHours).
        LivestockRules.PenaltyHours.Should().Be(SituationRules.ModifierHours);
        LivestockRules.HuntMinutes.Should().Be(30);
    }
}
