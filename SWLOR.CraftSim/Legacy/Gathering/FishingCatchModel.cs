using SWLOR.CraftSim.Data;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.CraftSim.Legacy.Gathering
{
    /// <summary>The catch roll of a bite (Fishing.cs:452-462); bait is spent and XP paid on every bite.</summary>
    public static class FishingCatchModel
    {
        public const int BaseChance = 40;
        public const int MaximumChance = 95;

        public static int CatchChancePercent(int agricultureRank, int fishLevel, int willpowerScore)
        {
            var chance = BaseChance + (agricultureRank - fishLevel) * 10 + willpowerScore * 2;
            if (chance > MaximumChance)
                chance = MaximumChance;
            else if (chance < 0)
                chance = 0;

            return chance;
        }

        /// <summary>XP for a bite, caught or not (Fishing.cs:491).</summary>
        public static int XpPerBite(int agricultureRank, int fishLevel) => ServerXp.ForDelta(fishLevel - agricultureRank);

        /// <summary>Every fish the game defines, from the fish enum.</summary>
        public static IReadOnlyList<FishInfo> Fish()
        {
            return Enum.GetValues<FishType>()
                .Where(f => f != FishType.Invalid)
                .Select(f => (Type: f, Attribute: f.GetAttribute<FishType, FishAttribute>()))
                .Select(f => new FishInfo(f.Type, f.Attribute.Name, f.Attribute.Level))
                .OrderBy(f => f.Level)
                .ThenBy(f => f.Name, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>The weighted fish lists of each location, rod and bait, with daytime and nighttime counted separately.</summary>
        public static IReadOnlyList<FishingPool> Pools()
        {
            var levels = Fish().ToDictionary(f => f.Type, f => f.Level);
            var pools = new List<FishingPool>();
            var definitions = typeof(IFishingLocationDefinition).Assembly.GetTypes()
                .Where(t => typeof(IFishingLocationDefinition).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .OrderBy(t => t.FullName, StringComparer.Ordinal);

            var merged = new Dictionary<FishingLocationType, FishingLocationDetail>();
            foreach (var definition in definitions)
            {
                var built = ((IFishingLocationDefinition)Activator.CreateInstance(definition)).Build();
                foreach (var (location, detail) in built)
                {
                    if (!merged.TryGetValue(location, out var existing))
                    {
                        merged[location] = detail;
                        continue;
                    }

                    foreach (var (key, list) in detail.FishMap)
                    {
                        if (!existing.FishMap.ContainsKey(key))
                            existing.FishMap[key] = new List<FishDetail>();

                        existing.FishMap[key].AddRange(list);
                    }
                }
            }

            foreach (var (location, detail) in merged.OrderBy(m => (int)m.Key))
            {
                foreach (var (key, list) in detail.FishMap.OrderBy(m => (int)m.Key.Item1).ThenBy(m => (int)m.Key.Item2))
                {
                    foreach (var timeOfDay in new[] { FishTimeOfDayType.Daytime, FishTimeOfDayType.Nighttime })
                    {
                        var available = list
                            .Where(f => f.TimeOfDay == FishTimeOfDayType.All || f.TimeOfDay == timeOfDay)
                            .Where(f => f.Frequency > 0)
                            .Select(f => new FishingPoolEntry(f.Type, levels[f.Type], f.Frequency))
                            .ToList();
                        if (available.Count > 0)
                            pools.Add(new FishingPool(location, key.Item1, key.Item2, timeOfDay, available));
                    }
                }
            }

            return pools;
        }

        /// <summary>The expected catch chance of a bite from a pool, in percent.</summary>
        public static double PoolCatchChance(FishingPool pool, int agricultureRank, int willpowerScore)
        {
            var total = pool.Entries.Sum(e => (double)e.Frequency);
            return pool.Entries.Sum(e => e.Frequency / total * CatchChancePercent(agricultureRank, e.Level, willpowerScore));
        }
    }
}
