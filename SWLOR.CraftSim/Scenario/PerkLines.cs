using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.CraftSim.Scenario
{
    /// <summary>
    /// Reads a perk line's stat totals from the game's own perk definitions, so a simulated build holds exactly the numbers the
    /// game grants and never a copy of them. A crafter owns a perk's highest rank whose skill requirement their rank meets.
    /// </summary>
    public static class PerkLines
    {
        // The game fills its stat metadata on first use, which is not safe from several threads: do it once, here, before any
        // parallel play reads a total.
        static PerkLines()
        {
            Stat.GetStatTypeAggregation(StatType.Invalid);
        }

        /// <summary>The stat totals of a crafter of <paramref name="rank"/> who has bought every perk the filter accepts.</summary>
        /// <param name="perks">The line's perks, as the definition builds them.</param>
        /// <param name="skill">The crafting skill whose rank the perks require.</param>
        /// <param name="rank">The crafter's rank in that skill.</param>
        /// <param name="include">Which perks the crafter owns; null for all of them.</param>
        public static IReadOnlyDictionary<StatType, int> TotalsAt(
            IReadOnlyDictionary<PerkType, PerkDetail> perks, SkillType skill, int rank, Func<PerkType, bool> include = null)
        {
            if (perks == null)
                throw new ArgumentNullException(nameof(perks));

            var totals = new Dictionary<StatType, int>();
            foreach (var (type, detail) in perks.OrderBy(pair => (int)pair.Key))
            {
                if (include != null && !include(type))
                    continue;

                var owned = OwnedLevel(detail, skill, rank);
                if (owned == null)
                    continue;

                foreach (var bonus in owned.StatBonuses)
                {
                    totals.TryGetValue(bonus.Stat, out var current);
                    totals[bonus.Stat] = Stat.AggregateStatAdjustment(bonus.Stat, current, bonus.Calculate(0));
                }
            }

            return totals;
        }

        /// <summary>The highest rank of the perk a crafter of <paramref name="rank"/> can own, or null when they cannot own any.</summary>
        public static PerkLevel OwnedLevel(PerkDetail detail, SkillType skill, int rank)
        {
            PerkLevel owned = null;
            foreach (var level in detail.PerkLevels.OrderBy(pair => pair.Key).Select(pair => pair.Value))
            {
                var required = level.Requirements.OfType<PerkRequirementSkill>().Where(r => r.Type == skill).Select(r => r.RequiredRank).DefaultIfEmpty(0).Max();
                if (required > rank)
                    break;

                owned = level;
            }

            return owned;
        }

        /// <summary>The perks of a definition, built without the engine (every perk names its own icon).</summary>
        public static IReadOnlyDictionary<PerkType, PerkDetail> Build(IPerkListDefinition definition) => definition.BuildPerks();

        public static Func<StatType, int> Lookup(IReadOnlyDictionary<StatType, int> totals) =>
            stat => totals.TryGetValue(stat, out var value) ? value : 0;
    }
}
