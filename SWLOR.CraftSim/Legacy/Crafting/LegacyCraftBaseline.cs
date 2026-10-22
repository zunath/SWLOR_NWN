using System.Collections.Concurrent;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Rng;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>Plays the old minigame at every recipe level, gear level and policy.</summary>
    public static class LegacyCraftBaseline
    {
        public static int RankFor(int recipeLevel, int offset) =>
            Math.Clamp(
                recipeLevel + offset,
                Math.Min(LegacyCraftRules.RequiredRank(recipeLevel), LegacyCraftRules.MaximumRank),
                LegacyCraftRules.MaximumRank);

        public static IReadOnlyList<CraftCell> Simulate(RecipeCatalog recipes, CraftingGearCatalog gear, LegacyCraftBaselineOptions options)
        {
            var work = new List<(SkillType Skill, int Level, int GearIndex, GearLevel Gear, int PolicyIndex, int Offset)>();
            foreach (var skill in options.Skills)
            {
                var levels = recipes.ForSkill(skill).Select(r => r.Level).Distinct().OrderBy(l => l).ToList();
                var gearLevels = gear.Levels(skill);
                foreach (var level in levels.Where(l => l >= 1))
                {
                    for (var gearIndex = 0; gearIndex < gearLevels.Count; gearIndex++)
                    {
                        for (var policyIndex = 0; policyIndex < options.Policies.Count; policyIndex++)
                        {
                            work.Add((skill, level, gearIndex, gearLevels[gearIndex], policyIndex, 0));
                            if (policyIndex != 0)
                                continue;

                            foreach (var offset in options.SensitivityOffsets.Where(o => o != 0).Distinct())
                                work.Add((skill, level, gearIndex, gearLevels[gearIndex], policyIndex, offset));
                        }
                    }
                }
            }

            var cells = new ConcurrentBag<(int Order, CraftCell Cell)>();
            Parallel.ForEach(work.Select((item, order) => (item, order)), entry =>
            {
                var (item, order) = entry;
                cells.Add((order, PlayCell(item.Skill, item.Level, item.GearIndex, item.Gear, item.PolicyIndex, item.Offset, options)));
            });

            return cells.OrderBy(c => c.Order).Select(c => c.Cell).ToList();
        }

        private static CraftCell PlayCell(
            SkillType skill, int level, int gearIndex, GearLevel gear, int policyIndex, int offset, LegacyCraftBaselineOptions options)
        {
            var rank = RankFor(level, offset);
            var policy = options.Policies[policyIndex];
            var context = new LegacyCraftContext(level, rank, gear.Control, gear.Craftsmanship, gear.CpBonus);
            var cell = new CraftCell
            {
                Skill = skill,
                RecipeLevel = level,
                Rank = rank,
                RankOffset = offset,
                Gear = gear.Name,
                Policy = policy.Name,
                Runs = options.Runs
            };

            for (var run = 0; run < options.Runs; run++)
            {
                var rng = SimRandom.ForRun(options.Seed, (int)skill, level, gearIndex, policyIndex, offset + 100, run);
                var result = LegacyCraftRunner.Play(context, policy, rng);
                cell.Actions += result.Actions;
                cell.CpSpent += result.CpSpent;
                if (!result.Succeeded)
                    continue;

                cell.Successes++;
                cell.QualitySum += result.QualityFraction;
                if (result.QualityFraction >= 1.0)
                    cell.FullQualitySuccesses++;
            }

            return cell;
        }
    }
}
