using SWLOR.CraftSim.Data;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Legacy.Crafting
{
    public static class CraftBandSummary
    {
        public static IReadOnlyList<CraftBandRow> Summarize(IReadOnlyList<CraftCell> cells, RecipeCatalog recipes, IReadOnlyList<RecipeBand> bands = null)
        {
            bands ??= RecipeBand.Default;
            var rows = new List<CraftBandRow>();
            var groups = cells
                .GroupBy(c => (c.Skill, c.Gear, c.Policy, c.RankOffset))
                .OrderBy(g => (int)g.Key.Skill);

            foreach (var group in groups)
            {
                var byLevel = group.ToDictionary(c => c.RecipeLevel);
                var recipesByLevel = recipes.ForSkill(group.Key.Skill).GroupBy(r => r.Level).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var band in bands)
                {
                    var row = Summarize(group.Key.Skill, group.Key.Gear, group.Key.Policy, group.Key.RankOffset, band, byLevel, recipesByLevel);
                    if (row != null)
                        rows.Add(row);
                }
            }

            return rows;
        }

        /// <summary>Combines rows of one band, gear level and policy from several skills, weighting each by its recipes.</summary>
        public static CraftBandRow Pool(IReadOnlyList<CraftBandRow> rows)
        {
            var recipes = rows.Sum(r => r.Recipes);
            var successes = rows.Sum(r => r.Recipes * r.SuccessRate);
            return new CraftBandRow
            {
                Skill = SkillType.Invalid,
                Band = rows[0].Band,
                Gear = rows[0].Gear,
                Policy = rows[0].Policy,
                RankOffset = rows[0].RankOffset,
                Recipes = recipes,
                Runs = rows[0].Runs,
                SuccessRate = successes / recipes,
                QualityFraction = successes > 0 ? rows.Sum(r => r.Recipes * r.SuccessRate * r.QualityFraction) / successes : 0,
                FullQualityShare = successes > 0 ? rows.Sum(r => r.Recipes * r.SuccessRate * r.FullQualityShare) / successes : 0,
                ActionsPerCraft = rows.Sum(r => r.Recipes * r.ActionsPerCraft) / recipes,
                StacksUsed = rows.Sum(r => r.StacksUsed),
                ItemsMade = rows.Sum(r => r.ItemsMade),
                NeverCraftedShare = rows.Sum(r => r.Recipes * r.NeverCraftedShare) / recipes
            };
        }

        private static CraftBandRow Summarize(
            SkillType skill,
            string gear,
            string policy,
            int offset,
            RecipeBand band,
            IReadOnlyDictionary<int, CraftCell> byLevel,
            IReadOnlyDictionary<int, List<RecipeEntry>> recipesByLevel)
        {
            double weight = 0, weightedSuccess = 0, weightedQuality = 0, weightedFull = 0, weightedActions = 0;
            double stacksUsed = 0, itemsMade = 0;
            var recipeCount = 0;
            var craftableRecipes = 0;
            var runs = 0;

            foreach (var (level, atLevel) in recipesByLevel.Where(l => band.Contains(l.Key)).OrderBy(l => l.Key))
            {
                if (!byLevel.TryGetValue(level, out var cell))
                    continue;

                var count = atLevel.Count;
                var successes = count * cell.SuccessRate;
                weight += count;
                weightedSuccess += successes;
                weightedQuality += successes * cell.QualityFraction;
                weightedFull += successes * cell.FullQualityShare;
                weightedActions += count * cell.ActionsPerCraft;
                recipeCount += count;
                runs = cell.Runs;

                if (cell.SuccessRate <= 0)
                    continue;

                // A craft consumes every stack when it succeeds and each at the failure loss chance when it does not.
                var lossShare = LegacyCraftRules.FailureLossPercent / 100.0;
                foreach (var recipe in atLevel)
                {
                    stacksUsed += recipe.ComponentStacks * (cell.SuccessRate + lossShare * (1 - cell.SuccessRate));
                    itemsMade += cell.SuccessRate;
                    craftableRecipes++;
                }
            }

            if (recipeCount == 0)
                return null;

            return new CraftBandRow
            {
                Skill = skill,
                Band = band.Name,
                Gear = gear,
                Policy = policy,
                RankOffset = offset,
                Recipes = recipeCount,
                Runs = runs,
                SuccessRate = weightedSuccess / weight,
                QualityFraction = weightedSuccess > 0 ? weightedQuality / weightedSuccess : 0,
                FullQualityShare = weightedSuccess > 0 ? weightedFull / weightedSuccess : 0,
                ActionsPerCraft = weightedActions / weight,
                StacksUsed = stacksUsed,
                ItemsMade = itemsMade,
                NeverCraftedShare = (recipeCount - craftableRecipes) / (double)recipeCount
            };
        }
    }
}
