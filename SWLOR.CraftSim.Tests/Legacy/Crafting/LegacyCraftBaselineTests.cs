using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Legacy.Crafting
{
    [TestFixture]
    public class LegacyCraftBaselineTests
    {
        private static RecipeEntry Recipe(SkillType skill, int level, params string[] components) =>
            new(default, skill, level, "item", default, components.ToDictionary(c => c, _ => 1));

        private static RecipeCatalog Catalog(params RecipeEntry[] recipes) => RecipeCatalog.From(recipes);

        private static CraftingGearCatalog NoGear => CraftingGearCatalog.Build(Array.Empty<GearItem>(), Catalog());

        [TestCase(10, 0, 10)]
        [TestCase(10, -3, 7)]
        [TestCase(10, -9, 7)]
        [TestCase(10, 5, 15)]
        [TestCase(48, 5, 50)]
        [TestCase(53, 0, 50)]
        [TestCase(2, -3, 0)]
        public void RankFor_KeepsTheRankInTheRecipesAllowedRange(int level, int offset, int expected)
        {
            LegacyCraftBaseline.RankFor(level, offset).Should().Be(expected);
        }

        [Test]
        public void Simulate_PlaysEveryLevelGearLevelAndPolicy_WithSensitivityOffsetsForTheFirstPolicyOnly()
        {
            var recipes = Catalog(Recipe(SkillType.Smithery, 10, "a"), Recipe(SkillType.Smithery, 40, "a"), Recipe(SkillType.Smithery, 40, "b"));
            var options = new LegacyCraftBaselineOptions
            {
                Runs = 5,
                Policies = new ILegacyCraftPolicy[] { new DominantRotationPolicy(), new RolloutPolicy() },
                Skills = new[] { SkillType.Smithery }
            };

            var cells = LegacyCraftBaseline.Simulate(recipes, NoGear, options);

            cells.Should().HaveCount(2 * (3 + 1), "two levels, one gear level, three ranks for the first policy and one for the second");
            cells.Where(c => c.Policy == "Lookahead").Should().OnlyContain(c => c.RankOffset == 0);
            cells.Where(c => c.Policy == "Dominant rotation").Select(c => c.RankOffset).Distinct().Should().BeEquivalentTo(new[] { -3, 0, 5 });
            cells.Should().OnlyContain(c => c.Runs == 5 && c.Gear == "no gear");
        }

        [Test]
        public void Simulate_RefusesABatchLargerThanTheCraftLimit()
        {
            var recipes = Catalog(Recipe(SkillType.Smithery, 10, "a"));
            var options = new LegacyCraftBaselineOptions { Runs = (int)(LegacyCraftBaseline.MaximumCrafts / 2) + 1, Skills = new[] { SkillType.Smithery } };

            var act = () => LegacyCraftBaseline.Simulate(recipes, NoGear, options);

            act.Should().Throw<ArgumentException>().WithMessage("*more than*crafts*");
        }

        [Test]
        public void Simulate_IsRepeatableFromTheSeedAndChangesWithIt()
        {
            var recipes = Catalog(Recipe(SkillType.Smithery, 30, "a"), Recipe(SkillType.Engineering, 45, "a"));
            LegacyCraftBaselineOptions Options(int seed) => new()
            {
                Runs = 30,
                Seed = seed,
                Policies = new ILegacyCraftPolicy[] { new DominantRotationPolicy(), new RolloutPolicy(4) },
                Skills = new[] { SkillType.Smithery, SkillType.Engineering }
            };

            var first = JsonConvert.SerializeObject(LegacyCraftBaseline.Simulate(recipes, SharedData.Gear, Options(1)));
            var again = JsonConvert.SerializeObject(LegacyCraftBaseline.Simulate(recipes, SharedData.Gear, Options(1)));
            var other = JsonConvert.SerializeObject(LegacyCraftBaseline.Simulate(recipes, SharedData.Gear, Options(2)));

            again.Should().Be(first);
            other.Should().NotBe(first);
        }

        [Test]
        public void Simulate_PlaysTheGearLevelsOfEachSkill()
        {
            var recipes = Catalog(Recipe(SkillType.Smithery, 30, "a"), Recipe(SkillType.Espionage, 30, "a"));
            var options = new LegacyCraftBaselineOptions
            {
                Runs = 3,
                SensitivityOffsets = Array.Empty<int>(),
                Skills = new[] { SkillType.Smithery, SkillType.Espionage }
            };

            var cells = LegacyCraftBaseline.Simulate(recipes, SharedData.Gear, options);

            cells.Where(c => c.Skill == SkillType.Smithery).Select(c => c.Gear).Should().Equal("no gear", "mid gear", "best gear");
            cells.Where(c => c.Skill == SkillType.Espionage).Select(c => c.Gear).Should().Equal("no gear");
        }

        [Test]
        public void ACell_CountsSuccessesQualityAndActionsOfItsCrafts()
        {
            var recipes = Catalog(Recipe(SkillType.Smithery, 40, "a"));
            var options = new LegacyCraftBaselineOptions { Runs = 50, SensitivityOffsets = Array.Empty<int>(), Skills = new[] { SkillType.Smithery } };

            var cell = LegacyCraftBaseline.Simulate(recipes, SharedData.Gear, options).Single(c => c.Gear == "best gear");

            cell.Runs.Should().Be(50);
            cell.Successes.Should().BeInRange(1, 50);
            cell.QualityFraction.Should().BeInRange(0, 1);
            cell.FullQualityShare.Should().BeInRange(0, 1);
            cell.ActionsPerCraft.Should().BeGreaterThan(5);
            cell.Rank.Should().Be(40);
            cell.SuccessRate.Should().Be(cell.Successes / 50.0);
        }

        [Test]
        public void Runner_CountsACraftAsFailedWhenThePolicyHasNothingToDo()
        {
            var context = new LegacyCraftContext(10, 10, 0, 0, 0);

            var result = LegacyCraftRunner.Play(context, new FixedPolicy(null), new SimRandom(1));

            (result.Succeeded, result.Actions).Should().Be((false, 0));
        }

        [Test]
        public void Runner_CountsACraftAsFailedWhenThePolicyChoosesARefusedAction()
        {
            var context = new LegacyCraftContext(10, 10, 0, 0, 0);

            var result = LegacyCraftRunner.Play(context, new FixedPolicy(LegacyAction.PreciseTouch), new SimRandom(1));

            (result.Succeeded, result.Actions, result.CpSpent).Should().Be((false, 0, 0));
        }

        [Test]
        public void Runner_ReportsCpSpentAndQualityOfASuccess()
        {
            var context = new LegacyCraftContext(1, 50, 100, 100, 100);

            var result = LegacyCraftRunner.Play(context, new DominantRotationPolicy(), new SimRandom(1));

            result.Succeeded.Should().BeTrue();
            result.QualityFraction.Should().BeInRange(0, 1);
            result.CpSpent.Should().BeInRange(0, context.MaxCp);
            result.Actions.Should().BeGreaterThan(0);
        }

        private sealed class FixedPolicy : ILegacyCraftPolicy
        {
            private readonly LegacyAction? _action;

            public FixedPolicy(LegacyAction? action)
            {
                _action = action;
            }

            public string Name => "Fixed";

            public LegacyAction? Choose(in LegacyCraftState state, LegacyCraftContext context, SimRandom rng) => _action;
        }
    }
}
