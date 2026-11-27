using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>The Performance skill rank a rider perk asks for, over a scripted perk catalog.</summary>
[TestFixture]
public class PerformanceRiderServiceTests
{
    [Test]
    public void ThePerkLevelOneSkillRequirementIsTheRequiredRank()
    {
        var perks = new Dictionary<PerkType, PerkDetail>
        {
            [PerkType.LightFeet] = Detail(Level(new PerkRequirementSkill(SkillType.Performance, 12), new PerkRequirementSkill(SkillType.Performance, 99))),
            [PerkType.Quickstep] = Detail(Level()),
            [PerkType.PowerStep] = new PerkDetail()
        };
        var riders = new TestRiders(perks);

        riders.GetRequiredRank(PerkType.LightFeet).Should().Be(12, "the first skill requirement of level one counts");
        riders.GetRequiredRank(PerkType.Quickstep).Should().Be(0, "a perk with no rank requirement asks for none");
        riders.GetRequiredRank(PerkType.PowerStep).Should().Be(0, "a perk without a level one asks for none");
        riders.GetRequiredRank(PerkType.VigorReel).Should().Be(0, "an unknown perk asks for none");
    }

    [Test]
    public void ARoutineOrSetIsUnlockedByItsPerk()
    {
        var riders = new TestRiders(new Dictionary<PerkType, PerkDetail>());
        riders.Levels[PerkType.LightFeet] = 1;
        riders.Levels[PerkType.KeenEar] = 2;

        riders.IsRoutineUnlocked(1, PerformanceRoutineType.LightFeet).Should().BeTrue();
        riders.IsRoutineUnlocked(1, PerformanceRoutineType.PowerStep).Should().BeFalse();
        riders.IsSetUnlocked(1, PerformanceSetType.KeenEar).Should().BeTrue();
        riders.IsSetUnlocked(1, PerformanceSetType.SteadyHands).Should().BeFalse();
    }

    [Test]
    public void ASecondRoutineNeedsItsOwnPerkAndTheCapstone()
    {
        var riders = new TestRiders(new Dictionary<PerkType, PerkDetail>());
        riders.Levels[PerkType.LightFeet] = 1;

        riders.CanBlendSecondRoutine(1, PerformanceRoutineType.LightFeet).Should().BeFalse("Showstopper is missing");
        riders.Levels[PerkType.Showstopper] = 1;
        riders.CanBlendSecondRoutine(1, PerformanceRoutineType.LightFeet).Should().BeTrue();
        riders.CanBlendSecondRoutine(1, PerformanceRoutineType.PowerStep).Should().BeFalse("the routine itself is locked");
    }

    [Test]
    public void ASecondSetNeedsItsOwnPerkAndTheCapstone()
    {
        var riders = new TestRiders(new Dictionary<PerkType, PerkDetail>());
        riders.Levels[PerkType.KeenEar] = 1;

        riders.CanWeaveSecondSet(1, PerformanceSetType.KeenEar).Should().BeFalse("Virtuoso is missing");
        riders.Levels[PerkType.Virtuoso] = 1;
        riders.CanWeaveSecondSet(1, PerformanceSetType.KeenEar).Should().BeTrue();
        riders.CanWeaveSecondSet(1, PerformanceSetType.SteadyHands).Should().BeFalse("the set itself is locked");
    }

    private static PerkDetail Detail(PerkLevel level)
    {
        var detail = new PerkDetail();
        detail.PerkLevels[1] = level;
        return detail;
    }

    private static PerkLevel Level(params IPerkRequirement[] requirements)
    {
        var level = new PerkLevel();
        level.Requirements.AddRange(requirements);
        return level;
    }

    private sealed class TestRiders : PerformanceRiderService
    {
        private readonly Dictionary<PerkType, PerkDetail> _perks;

        public TestRiders(Dictionary<PerkType, PerkDetail> perks)
            : base(FacadeForwarder.For<IPerkService>()) => _perks = perks;

        public Dictionary<PerkType, int> Levels { get; } = new();

        protected override int GetPerkLevel(uint creature, PerkType perk) => Levels.GetValueOrDefault(perk);

        protected override bool TryGetPerkDetails(PerkType perk, out PerkDetail detail) => _perks.TryGetValue(perk, out detail!);
    }
}
