using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PerformanceService;
using static SWLOR.Game.Server.Tests.Service.PerformanceTestSnapshots;

namespace SWLOR.Game.Server.Tests.Service;

public class PerformanceInspiredComposerTests
{
    [Test]
    public void Compose_WithNobodyFollowedGrantsNothing()
    {
        var data = PerformanceInspiredComposer.Compose(null, null, 5);

        data.XPBonusPercent.Should().Be(0);
        data.StandingOvation.Should().BeFalse();
        data.Requiem.Should().BeFalse();
    }

    [Test]
    public void Compose_AddsBothScaledBonusesAndOnePercentPerVenuePoint()
    {
        var dancer = Dancer(1, xpScaled: 22);
        var musician = Musician(2, xpScaled: 15);

        PerformanceInspiredComposer.Compose(dancer, musician, 0).XPBonusPercent.Should().Be(37);
        PerformanceInspiredComposer.Compose(dancer, musician, 4).XPBonusPercent.Should().Be(41);
        PerformanceInspiredComposer.Compose(dancer, null, 3).XPBonusPercent.Should().Be(25);
        PerformanceInspiredComposer.Compose(null, musician, 9).XPBonusPercent.Should().Be(20);
    }

    [Test]
    public void Compose_LightFeetGrantsTheDancersOutOfCombatSpeed()
    {
        var dancer = Dancer(1, routine: PerformanceRoutineType.LightFeet, lightFeetSpeedPercent: 15);

        PerformanceInspiredComposer.Compose(dancer, null, 0).OutOfCombatMovementSpeedPercent.Should().Be(15);
    }

    [TestCase(PerformanceRoutineType.PowerStep, 0, 1, 0, 0)]
    [TestCase(PerformanceRoutineType.PowerStep, 1, 2, 0, 0)]
    [TestCase(PerformanceRoutineType.Quickstep, 0, 0, 1, 0)]
    [TestCase(PerformanceRoutineType.Quickstep, 1, 0, 2, 0)]
    [TestCase(PerformanceRoutineType.VigorReel, 0, 0, 0, 1)]
    [TestCase(PerformanceRoutineType.VigorReel, 1, 0, 0, 2)]
    public void Compose_AttributeRoutinesGrantOnePointOrTwoWithPeakForm(
        PerformanceRoutineType routine, int peakForm, int might, int agility, int vitality)
    {
        var dancer = Dancer(1, routine: routine, routineAttributeBonus: peakForm);

        var data = PerformanceInspiredComposer.Compose(dancer, null, 0);

        data.Might.Should().Be(might);
        data.Agility.Should().Be(agility);
        data.Vitality.Should().Be(vitality);
    }

    [Test]
    public void Compose_CantinaShuffleCarriesNoRider()
    {
        var data = PerformanceInspiredComposer.Compose(Dancer(1, xpScaled: 6), Musician(2, xpScaled: 6), 0);

        data.Should().Be(new SWLOR.Game.Server.Service.StatusEffectService.InspiredEffectData { XPBonusPercent = 12 });
    }

    [TestCase(PerformanceSetType.KeenEar, 0, 1, 0, 0)]
    [TestCase(PerformanceSetType.KeenEar, 1, 2, 0, 0)]
    [TestCase(PerformanceSetType.StillWaters, 0, 0, 1, 0)]
    [TestCase(PerformanceSetType.StillWaters, 1, 0, 2, 0)]
    [TestCase(PerformanceSetType.SilverTongue, 0, 0, 0, 1)]
    [TestCase(PerformanceSetType.SilverTongue, 1, 0, 0, 2)]
    public void Compose_AttributeSetsGrantOnePointOrTwoWithPerfectPitch(
        PerformanceSetType set, int perfectPitch, int perception, int willpower, int social)
    {
        var musician = Musician(1, set: set, setAttributeBonus: perfectPitch);

        var data = PerformanceInspiredComposer.Compose(null, musician, 0);

        data.Perception.Should().Be(perception);
        data.Willpower.Should().Be(willpower);
        data.Social.Should().Be(social);
    }

    [Test]
    public void Compose_SteadyHandsAndAnglersLullabyUseTheMusiciansOwnStats()
    {
        var steady = Musician(1, set: PerformanceSetType.SteadyHands, steadyHandsBonus: 4);
        var angler = Musician(2, set: PerformanceSetType.AnglersLullaby, anglersLullabyBonus: 10);

        PerformanceInspiredComposer.Compose(null, steady, 0).CraftBonus.Should().Be(4);
        PerformanceInspiredComposer.Compose(null, angler, 0).FishingCatchChancePercent.Should().Be(10);
    }

    [Test]
    public void Compose_CarriesStandingOvationFromTheDancerAndRequiemFromTheMusician()
    {
        var dancer = Dancer(1, hasStandingOvation: true);
        var musician = Musician(2, hasRequiem: true);

        var both = PerformanceInspiredComposer.Compose(dancer, musician, 0);
        both.StandingOvation.Should().BeTrue();
        both.Requiem.Should().BeTrue();

        var plain = PerformanceInspiredComposer.Compose(Dancer(3), Musician(4), 0);
        plain.StandingOvation.Should().BeFalse();
        plain.Requiem.Should().BeFalse();
    }

    [Test]
    public void Compose_ADancerWithoutTheCapstoneIgnoresASecondRoutine()
    {
        var dancer = Dancer(
            1, routine: PerformanceRoutineType.PowerStep, secondRoutine: PerformanceRoutineType.Quickstep, hasCapstone: false);

        var data = PerformanceInspiredComposer.Compose(dancer, null, 0);

        data.Might.Should().Be(1);
        data.Agility.Should().Be(0);
    }

    [Test]
    public void Compose_ShowstopperBlendsTwoRoutinesAndVirtuosoTwoSets()
    {
        var dancer = Dancer(
            1, routine: PerformanceRoutineType.PowerStep, secondRoutine: PerformanceRoutineType.Quickstep, hasCapstone: true);
        var musician = Musician(
            2, set: PerformanceSetType.KeenEar, secondSet: PerformanceSetType.SilverTongue, hasCapstone: true);

        var data = PerformanceInspiredComposer.Compose(dancer, musician, 0);

        data.Might.Should().Be(1);
        data.Agility.Should().Be(1);
        data.Perception.Should().Be(1);
        data.Social.Should().Be(1);
    }

    [Test]
    public void Compose_PartnerDanceSharesTheOtherDancersRoutineWhenEitherHasTheTrait()
    {
        var dancer = Dancer(1, routine: PerformanceRoutineType.PowerStep, hasPartnerTrait: true);
        var partner = Dancer(2, routine: PerformanceRoutineType.LightFeet, lightFeetSpeedPercent: 10, x: 3f);
        PerformanceShow.Prepare(new[] { dancer, partner });

        // Followers of either dancer get both routines, because one of the two has Partner Dance.
        var fromDancer = PerformanceInspiredComposer.Compose(dancer, null, 0);
        var fromPartner = PerformanceInspiredComposer.Compose(partner, null, 0);

        fromDancer.Might.Should().Be(1);
        fromDancer.OutOfCombatMovementSpeedPercent.Should().Be(10);
        fromPartner.Might.Should().Be(1);
        fromPartner.OutOfCombatMovementSpeedPercent.Should().Be(10);
    }

    [Test]
    public void Compose_WithoutPartnerDanceTheOtherDancersRoutineIsNotShared()
    {
        var dancer = Dancer(1, routine: PerformanceRoutineType.PowerStep);
        var partner = Dancer(2, routine: PerformanceRoutineType.LightFeet, lightFeetSpeedPercent: 10, x: 3f);
        PerformanceShow.Prepare(new[] { dancer, partner });

        var data = PerformanceInspiredComposer.Compose(dancer, null, 0);

        data.Might.Should().Be(1);
        data.OutOfCombatMovementSpeedPercent.Should().Be(0);
    }

    [Test]
    public void Compose_APartnersRoutineKeepsThePartnersOwnPeakFormBonus()
    {
        var dancer = Dancer(1, routine: PerformanceRoutineType.PowerStep, hasPartnerTrait: true);
        var partner = Dancer(2, routine: PerformanceRoutineType.Quickstep, routineAttributeBonus: 1, x: 3f);
        PerformanceShow.Prepare(new[] { dancer, partner });

        var data = PerformanceInspiredComposer.Compose(dancer, null, 0);

        data.Might.Should().Be(1);
        data.Agility.Should().Be(2);
    }

    [Test]
    public void Compose_DuetSharesABandPartnersSet()
    {
        var musician = Musician(1, set: PerformanceSetType.KeenEar, hasPartnerTrait: true);
        var bandmate = Musician(2, set: PerformanceSetType.SteadyHands, steadyHandsBonus: 2, x: 4f);
        PerformanceShow.Prepare(new[] { musician, bandmate });

        var data = PerformanceInspiredComposer.Compose(null, bandmate, 0);

        data.Perception.Should().Be(1);
        data.CraftBonus.Should().Be(2);
    }

    [Test]
    public void Riders_NeverExceedTwoRoutinesOrTwoSets()
    {
        var dancer = Dancer(
            1, routine: PerformanceRoutineType.PowerStep, secondRoutine: PerformanceRoutineType.Quickstep,
            hasCapstone: true, hasPartnerTrait: true);
        var partner = Dancer(
            2, routine: PerformanceRoutineType.VigorReel, secondRoutine: PerformanceRoutineType.LightFeet,
            hasCapstone: true, x: 3f);
        var musician = Musician(
            3, set: PerformanceSetType.KeenEar, secondSet: PerformanceSetType.StillWaters,
            hasCapstone: true, hasPartnerTrait: true);
        var bandmate = Musician(
            4, set: PerformanceSetType.SilverTongue, secondSet: PerformanceSetType.AnglersLullaby,
            hasCapstone: true, x: 3f);
        PerformanceShow.Prepare(new[] { dancer, partner, musician, bandmate });

        var routines = PerformanceRiders.GetRoutineRiders(dancer);
        var sets = PerformanceRiders.GetSetRiders(musician);

        routines.Select(rider => rider.Routine)
            .Should().Equal(PerformanceRoutineType.PowerStep, PerformanceRoutineType.Quickstep);
        sets.Select(rider => rider.Set)
            .Should().Equal(PerformanceSetType.KeenEar, PerformanceSetType.StillWaters);

        var data = PerformanceInspiredComposer.Compose(dancer, musician, 0);
        data.Vitality.Should().Be(0);
        data.Social.Should().Be(0);
    }

    [Test]
    public void Riders_ACandidateRoutineAlreadyOnTheListIsNotAddedTwice()
    {
        var dancer = Dancer(1, routine: PerformanceRoutineType.PowerStep, hasPartnerTrait: true);
        var partner = Dancer(2, routine: PerformanceRoutineType.PowerStep, secondRoutine: PerformanceRoutineType.VigorReel,
            hasCapstone: true, x: 3f);
        PerformanceShow.Prepare(new[] { dancer, partner });

        var routines = PerformanceRiders.GetRoutineRiders(dancer);

        routines.Select(rider => rider.Routine)
            .Should().Equal(PerformanceRoutineType.PowerStep, PerformanceRoutineType.VigorReel);
        PerformanceInspiredComposer.Compose(dancer, null, 0).Might.Should().Be(1);
    }

    [Test]
    public void Riders_ANullPerformerHasNone()
    {
        PerformanceRiders.GetRoutineRiders(null).Should().BeEmpty();
        PerformanceRiders.GetSetRiders(null).Should().BeEmpty();
    }
}
