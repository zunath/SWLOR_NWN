using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Tests.Support;
using static SWLOR.CraftSim.Tests.Support.BenchHarness;

namespace SWLOR.CraftSim.Tests.Gates
{
    [TestFixture]
    public class GearLadderGateTests
    {
        private static readonly int[] TopLevels = { 48, 52 };

        [Test]
        public void OnlyAMinigameTunedAgainstFullModsHasTheGate()
        {
            Suite(new BenchScenario()).Gates.OfType<GearLadderGate>().Should().BeEmpty();
            Suite(new BenchScenario { TunedAgainstFullMods = true }).Gates.OfType<GearLadderGate>().Should().ContainSingle();
        }

        [Test]
        public void ItPlaysTheBandsItNamesInEveryGearStepUpToFullMods()
        {
            var result = Evaluate(GearLadderGate.Key, new BenchScenario { TunedAgainstFullMods = true }, Context(TopLevels));

            result.Gate.Should().Be("Gear ladder");
            result.Tables[0].Rows.Select(row => row[0]).Distinct().Should().Equal("46-50", "51-53");
            result.Tables[0].Rows.Select(row => row[3]).Should().Contain(CraftingGearCatalog.FullyModdedGearName);
            result.Tables[1].Title.Should().Contain("Perks do not replace gear");
        }

        [Test]
        public void AStepThatAddsTooLittleFailsAndNamesTheStep()
        {
            // No step can add this much, and nothing counts as saturated.
            var unreachable = GateThresholds.Default with { GearLadderMinimumStep = 1000, GearLadderSaturatedQuality = 101 };

            var result = Evaluate(GearLadderGate.Key, new BenchScenario { TunedAgainstFullMods = true }, Context(TopLevels), thresholds: unreachable);

            result.Verdict.Should().Be(GateVerdict.Failed);
            result.Summary.Should().Contain("over the gear below it");
        }

        [Test]
        public void TheTopBandsTargetsAreTheNamedThresholds()
        {
            var impossible = GateThresholds.Default with { GearLadderFullModsReach85Share = 1.01, GearLadderFullModsMaxFullQualityShare = -1 };

            var result = Evaluate(GearLadderGate.Key, new BenchScenario { TunedAgainstFullMods = true }, Context(TopLevels), thresholds: impossible);

            result.Summary.Should().Contain("reaches 85% on only").And.Contain("reaches 100% on");
        }

        [Test]
        public void TheGearLadderIsTheOldLadderPlusFullMods()
        {
            var plain = SharedData.Gear.Levels(SWLOR.Game.Server.Service.SkillService.SkillType.Smithery);
            var modded = SharedData.Gear.LevelsWithFullMods(SWLOR.Game.Server.Service.SkillService.SkillType.Smithery);

            modded.Should().HaveCount(plain.Count + 1);
            modded[^1].Name.Should().Be(CraftingGearCatalog.FullyModdedGearName);
            modded[^1].Control.Should().Be(plain[^1].Control + CraftingGearCatalog.FullyModdedPointsPerStat);
            modded[^1].Craftsmanship.Should().Be(plain[^1].Craftsmanship + CraftingGearCatalog.FullyModdedPointsPerStat);
            modded[^1].CpBonus.Should().Be(plain[^1].CpBonus, "kits add no CP");
        }
    }
}
