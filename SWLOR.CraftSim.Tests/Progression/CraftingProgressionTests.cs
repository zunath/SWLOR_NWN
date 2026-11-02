using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Progression;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Progression
{
    [TestFixture]
    public class CraftingProgressionTests
    {
        private static readonly IReadOnlyDictionary<string, int> Components = new Dictionary<string, int>
        {
            { "ref_veldite", 2 },
            { "wood", 1 }
        };

        private static readonly ProgressionAssumptions Assumptions = new();

        private static SkillPath PerfectCrafter(ProgressionAssumptions assumptions = null) =>
            ProgressionFixture.HandMade(SkillType.Smithery, Components, 1.0, 0.0, 10.0).Path(SkillType.Smithery, ProgressionFixture.Gear, assumptions ?? Assumptions);

        [Test]
        public void TheClimbCoversRanksZeroToFortyNineAndCostsTheServersXp()
        {
            var path = PerfectCrafter();

            path.Steps.Select(s => s.Rank).Should().Equal(Enumerable.Range(0, 50));
            path.Steps.Select(s => s.RequiredXp).Should().Equal(Enumerable.Range(0, 50).Select(Skill.GetRequiredXP));
            path.RequiredXp.Should().Be(411_125);
        }

        [Test]
        public void RankZeroPaysTheLevelOnePriceAndEveryLaterRankPaysTheAtLevelPrice()
        {
            var steps = PerfectCrafter().Steps;

            steps[0].RecipeLevel.Should().Be(1);
            steps[0].BaseXp.Should().Be(676, "GetDeltaXP(+1)");
            steps[1].BaseXp.Should().Be(600, "GetDeltaXP(0)");
            steps.Skip(1).Select(s => s.BaseXp).Should().OnlyContain(xp => xp == 600);
            steps.Skip(1).Select(s => s.RecipeLevel).Should().Equal(Enumerable.Range(1, 49));
        }

        [Test]
        public void HoursFollowFromTheCraftsTheActionsAndTheTimings()
        {
            var assumptions = Assumptions with { OverheadSecondsPerCraft = 30, SecondsPerAction = 6 };
            var step = PerfectCrafter(assumptions).Steps[10];

            // A perfect crafter at zero quality earns 600 XP a craft and consumes every committed stack.
            step.Crafts.Should().BeApproximately(Skill.GetRequiredXP(10) / 600.0, 1e-9);
            step.CraftHours.Should().BeApproximately(step.Crafts * (10 * 6 + 30) / 3600.0, 1e-9);

            var supply = ProgressionFixture.Supply;
            var gatherSeconds = 2 * supply.SecondsPerUnit("ref_veldite", assumptions) + supply.SecondsPerUnit("wood", assumptions);
            step.GatherHours.Should().BeApproximately(step.Crafts * gatherSeconds / 3600.0, 1e-9);
            step.ModelledUnits.Should().BeApproximately(step.Crafts * 3, 1e-9);
            step.UnmodelledUnits.Should().Be(0);
            step.TotalHours.Should().BeApproximately(step.CraftHours + step.GatherHours, 1e-12);
        }

        [Test]
        public void SuccessQualityAndFailureSetTheXpPerCraftAndTheMaterialsConsumed()
        {
            var model = ProgressionFixture.HandMade(SkillType.Smithery, Components, 0.8, 0.5, 12.0);
            var step = model.Path(SkillType.Smithery, ProgressionFixture.Gear, Assumptions).Steps[20];

            var success = CraftOutcomeRules.CalculateXp(600, 0, false, 0.5f);
            var failure = CraftOutcomeRules.CalculateFailureXp(600, 0);
            var xpPerCraft = 0.8 * success + 0.2 * failure;
            step.Crafts.Should().BeApproximately(Skill.GetRequiredXP(20) / xpPerCraft, 1e-9);

            // A failure loses each stack on 65% of rolls, so a craft consumes 0.8 + 0.65 x 0.2 of the committed units.
            step.ModelledUnits.Should().BeApproximately(step.Crafts * 3 * (0.8 + 0.65 * 0.2), 1e-9);
        }

        [Test]
        public void ACraftingOnlyMultiplierDividesTheHoursByItself()
        {
            var plain = PerfectCrafter();
            var doubled = PerfectCrafter(Assumptions with { CraftXpMultiplier = 2.0 });

            doubled.TotalHours.Should().BeApproximately(plain.TotalHours / 2, 1e-9);
            doubled.Crafts.Should().BeApproximately(plain.Crafts / 2, 1e-9);
            doubled.RequiredXp.Should().Be(plain.RequiredXp, "a multiplier changes what a craft pays, not what a rank costs");
        }

        [Test]
        public void ACapOnTheRankCostFlattensOnlyTheExpensiveRanks()
        {
            var plain = PerfectCrafter();
            var capped = PerfectCrafter(Assumptions with { RequiredXpCap = 5000 });

            capped.Steps.Select(s => s.RequiredXp).Should().Equal(plain.Steps.Select(s => Math.Min(s.RequiredXp, 5000)));
            capped.Steps[2].Crafts.Should().BeApproximately(plain.Steps[2].Crafts, 1e-12, "rank 2 costs less than the cap");
            capped.Steps[40].Crafts.Should().BeLessThan(plain.Steps[40].Crafts);
            capped.TotalHours.Should().BeLessThan(plain.TotalHours);
        }

        [Test]
        public void RotatingThroughTheLevelsRecipesEarnsTheFirstCraftBonusOnEachOnce()
        {
            var model = ProgressionFixture.HandMade(SkillType.Smithery, Components, 1.0, 0.0, 10.0, recipesPerLevel: 3);
            var plain = model.Path(SkillType.Smithery, ProgressionFixture.Gear, Assumptions);
            var variety = model.Path(SkillType.Smithery, ProgressionFixture.Gear, Assumptions with { VarietyPlay = true });

            // Rank 30 costs 10,030 XP, far more than three first crafts (3 x 720 XP), so only the first three crafts gain.
            var gained = 3 * (CraftOutcomeRules.CalculateXp(600, 0, true, 0f) - 600);
            plain.Steps[30].Crafts.Should().BeApproximately(Skill.GetRequiredXP(30) / 600.0, 1e-9);
            (plain.Steps[30].Crafts - variety.Steps[30].Crafts).Should().BeApproximately(gained / 600.0, 1e-6);
            variety.TotalHours.Should().BeLessThan(plain.TotalHours);
        }

        [Test]
        public void ComponentsNoNodeSuppliesAreCountedAndOnlyTimedWhenAsked()
        {
            var components = new Dictionary<string, int> { { "wood", 1 }, { "lth_ruined", 3 } };
            var model = ProgressionFixture.HandMade(SkillType.Smithery, components, 1.0, 0.0, 10.0);

            var left = model.Path(SkillType.Smithery, ProgressionFixture.Gear, Assumptions).Steps[10];
            left.ModelledUnits.Should().BeApproximately(left.Crafts, 1e-9);
            left.UnmodelledUnits.Should().BeApproximately(left.Crafts * 3, 1e-9);

            var priced = model.Path(SkillType.Smithery, ProgressionFixture.Gear, Assumptions with { UnmodelledUnitSeconds = 20 }).Steps[10];
            (priced.GatherHours - left.GatherHours).Should().BeApproximately(priced.Crafts * 3 * 20 / 3600.0, 1e-9);
        }

        [Test]
        public void CraftingTheCheapestQuarterOfALevelsRecipesNeedsLessGathering()
        {
            var recipes = RecipeCatalog.From(Enumerable.Range(1, 53).SelectMany(level => new[]
            {
                Recipe(level, "cheap", new Dictionary<string, int> { { "wood", 1 } }),
                Recipe(level, "dear_a", new Dictionary<string, int> { { "wood", 6 } }),
                Recipe(level, "dear_b", new Dictionary<string, int> { { "wood", 6 } }),
                Recipe(level, "dear_c", new Dictionary<string, int> { { "wood", 6 } })
            }));
            var model = ProgressionFixture.Model(recipes, ComponentSupply.Build(recipes), null, 1.0, 0.0, 10.0, new[] { SkillType.Smithery });

            var average = model.Path(SkillType.Smithery, ProgressionFixture.Gear, Assumptions).Steps[10];
            var cheapest = model.Path(SkillType.Smithery, ProgressionFixture.Gear, Assumptions with { CheapestQuartileRecipes = true }).Steps[10];

            // Average of 1 and three 6s is 4.75 woods; the cheapest quarter is the single 1-wood recipe.
            (average.GatherHours / cheapest.GatherHours).Should().BeApproximately(4.75, 1e-9);
            cheapest.CraftHours.Should().BeApproximately(average.CraftHours, 1e-12);
        }

        [Test]
        public void ThePathIsRepeatable()
        {
            ProgressionFixture.RealRecipes.Path(SkillType.Fabrication, ProgressionFixture.Gear, Assumptions).TotalHours
                .Should().Be(ProgressionFixture.RealRecipes.Path(SkillType.Fabrication, ProgressionFixture.Gear, Assumptions).TotalHours);
        }

        [Test]
        public void ThePaceOfARealSkillIsAboveRpXpWhenCraftsAreFastAndBelowItWhenGatheringIsSlow()
        {
            var fast = ProgressionFixture.RealRecipes.Path(SkillType.Agriculture, ProgressionFixture.Gear, Assumptions with { NodeTravelSeconds = 1 });
            var slow = ProgressionFixture.RealRecipes.Path(SkillType.Smithery, ProgressionFixture.Gear, Assumptions with { NodeTravelSeconds = 600 });

            fast.XpPerHour.Should().BeGreaterThan(ProgressionAssumptions.RoleplayXpPerHour);
            slow.XpPerHour.Should().BeLessThan(ProgressionAssumptions.RoleplayXpPerHour);
        }

        [Test]
        public void TheDefaultActionTimeIsTheLegacyWindowsEstimate()
        {
            new ProgressionAssumptions().SecondsPerAction.Should().Be(GateThresholds.Default.LegacySecondsPerAction);
        }

        private static RecipeEntry Recipe(int level, string name, IReadOnlyDictionary<string, int> components) =>
            new(default, SkillType.Smithery, level, $"{name}_{level}", default, components);
    }
}
