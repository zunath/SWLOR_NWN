using System.Collections;
using System.Reflection;
using SWLOR.CraftSim.Data;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.CraftSim.Legacy.Gathering
{
    /// <summary>
    /// Refining ore with power cores (RefineryViewModel.cs): 3 + Refinery Management units per core, and a flat XP per unit from the
    /// refinery's ore table that the refiner's rank scales (<see cref="CurrentXpPerUnit"/>). The ore list, tiers and flat XP are read from
    /// the refinery itself.
    /// </summary>
    public static class RefiningModel
    {
        public const int BaseItemsPerCore = 3;
        public const int MaximumRefineryManagement = 6;
        private const int UnavailableLevel = 99;

        /// <summary>RefineryViewModel.cs:173-174.</summary>
        public static int ItemsPerCore(int refineryManagement) => BaseItemsPerCore + refineryManagement;

        public static int CoresRequired(int units, int refineryManagement) =>
            (int)Math.Ceiling(units / (float)ItemsPerCore(refineryManagement));

        /// <summary>The flat XP per unit of the refinery's ore table (RefineryViewModel.cs:35-52): 25 to 150 by ore. It is what refining paid whatever the rank, and pays at the ore's level now.</summary>
        public static int Xp(OreTier ore, int units) => ore.XpPerUnit * units;

        /// <summary>
        /// The XP of one unit refining pays now: the ore's flat XP times <c>GetDeltaXP(ore level - rank) / GetDeltaXP(0)</c>, at the level of the
        /// vein of the ore's tier (<see cref="GatheringLevels.RefiningXp"/>, RefineryViewModel.cs:356). It is the flat XP at the ore's level.
        /// </summary>
        public static int CurrentXpPerUnit(OreTier ore, int gatheringRank) => CurrentXp(ore, gatheringRank, 1);

        /// <summary>The XP refining a stack of units of the ore pays a refiner of the rank, as the refinery rounds it.</summary>
        public static int CurrentXp(OreTier ore, int gatheringRank, int units)
        {
            // The server's XP chart is cached on first use.
            ServerXp.ForDelta(0);
            return GatheringLevels.RefiningXp(ore.XpPerUnit, ore.RequiredLevel, gatheringRank, units);
        }

        public static IReadOnlyList<OreTier> Ores() => LoadOres();

        private static List<OreTier> LoadOres()
        {
            var field = typeof(RefineryViewModel).GetField("_ores", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidDataException("RefineryViewModel no longer has an _ores table; update RefiningModel.");

            var ores = new List<OreTier>();
            foreach (DictionaryEntry entry in (IDictionary)field.GetValue(null)!)
            {
                var detail = entry.Value!;
                var type = detail.GetType();
                var level = (int)type.GetProperty("RequiredLevel")!.GetValue(detail)!;
                if (level >= UnavailableLevel)
                    continue;

                ores.Add(new OreTier(
                    (string)entry.Key,
                    (string)type.GetProperty("RefinedItemResref")!.GetValue(detail)!,
                    level,
                    (int)type.GetProperty("XPGranted")!.GetValue(detail)!));
            }

            return ores.OrderBy(o => o.RequiredLevel).ThenBy(o => o.XpPerUnit).ThenBy(o => o.Ore, StringComparer.Ordinal).ToList();
        }
    }
}
