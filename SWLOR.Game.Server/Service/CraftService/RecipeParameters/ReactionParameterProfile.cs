using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.RecipeParameters
{
    /// <summary>
    /// Turns an Espionage recipe into Reaction parameters. Poisons and traps use different cycle patterns, the cycle length varies
    /// from recipe to recipe, and the recipe's tier sets how violent the Peak is.
    /// </summary>
    public sealed class ReactionParameterProfile : RecipeParameterProfile<ReactionParameters>
    {
        private static readonly IReadOnlyList<RecipeParameterDefinition> _definitions = new[]
        {
            RecipeParameterDefinition.Enumeration<ReactionCyclePattern>("cyclePattern", "Reaction Cycle Pattern"),
            RecipeParameterDefinition.Integer("cycleLength", "Reaction Cycle Length", ReactionCycle.MinimumLength, ReactionCycle.MaximumLength, 1),
            RecipeParameterDefinition.Integer("peakIntensity", "Reaction Peak Intensity", 1, 5)
        };

        private static readonly HashSet<RecipeType> ConcentratedVenoms = new()
        {
            RecipeType.WhisperthornConcentrate,
            RecipeType.GlassfangConcentrate,
            RecipeType.TombsporeConcentrate,
            RecipeType.RimevenomConcentrate,
            RecipeType.NightrootConcentrate
        };

        /// <summary>The highest recipe level of tiers 1 to 4: levels up to 9 are tier 1, up to 22 tier 2, up to 33 tier 3, up to 45 tier 4, and above that tier 5.</summary>
        private static readonly int[] TierLevelLimits = { 9, 22, 33, 45 };

        /// <summary>Difficulty is 10 per point of Peak intensity, plus 3 for each Peak turn beyond the first, plus 1 for each turn the cycle is shorter than 8.</summary>
        private const double IntensityDifficulty = 10;
        private const double ExtraPeakTurnDifficulty = 3;
        private const double ShortCycleDifficulty = 1;

        public override string Key => "Reaction";

        public override SkillType Skill => SkillType.Espionage;

        public override IReadOnlyList<RecipeParameterDefinition> Definitions => _definitions;

        /// <summary>The Espionage tier of a recipe level, 1 to 5.</summary>
        public static int TierForLevel(int level)
        {
            var tier = 1;
            foreach (var limit in TierLevelLimits)
            {
                if (level > limit)
                    tier++;
            }

            return tier;
        }

        public override IReadOnlyDictionary<string, double> BaseValues(RecipeParameterContext recipe)
        {
            var tier = TierForLevel(recipe.Level);
            var pattern = PatternFor(recipe);
            var length = BaseLength(pattern) + (tier == 1 ? 1 : 0) - (tier == 5 ? 1 : 0);

            return new Dictionary<string, double>
            {
                ["cyclePattern"] = (int)pattern,
                ["cycleLength"] = length,
                ["peakIntensity"] = tier
            };
        }

        protected override ReactionParameters Create(RecipeParameterValues values)
        {
            return new ReactionParameters(
                values.EnumValue<ReactionCyclePattern>("cyclePattern"),
                values.Int("cycleLength"),
                values.Int("peakIntensity"));
        }

        protected override double MeasureDifficulty(ReactionParameters parameters)
        {
            var cycle = new ReactionCycle(parameters.CyclePattern, parameters.CycleLength);
            return IntensityDifficulty * parameters.PeakIntensity +
                   ExtraPeakTurnDifficulty * (cycle.TurnsIn(ReactionPhase.Peak) - 1) +
                   ShortCycleDifficulty * (ReactionCycle.MaximumLength - parameters.CycleLength);
        }

        public override DifficultyBudget BudgetFor(int recipeLevel)
        {
            var floor = IntensityDifficulty * TierForLevel(recipeLevel);
            return new DifficultyBudget(floor, floor + 8);
        }

        private static ReactionCyclePattern PatternFor(RecipeParameterContext recipe)
        {
            if (recipe.Category == RecipeCategoryType.Tool)
                return ReactionCyclePattern.Trigger;

            return ConcentratedVenoms.Contains(recipe.Type) ? ReactionCyclePattern.Surge : ReactionCyclePattern.Brew;
        }

        private static int BaseLength(ReactionCyclePattern pattern) => pattern switch
        {
            ReactionCyclePattern.Trigger => 5,
            _ => 6
        };
    }
}
