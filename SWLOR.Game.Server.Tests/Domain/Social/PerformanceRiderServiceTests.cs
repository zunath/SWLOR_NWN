using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

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

        public TestRiders(Dictionary<PerkType, PerkDetail> perks) => _perks = perks;

        protected override bool TryGetPerkDetails(PerkType perk, out PerkDetail detail) => _perks.TryGetValue(perk, out detail!);
    }
}
