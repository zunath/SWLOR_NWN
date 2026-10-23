using System.Collections;
using System.Reflection;
using SWLOR.CraftSim.Data;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.CraftSim.Legacy.Gathering
{
    /// <summary>
    /// Refining ore with power cores (RefineryViewModel.cs): 3 + Refinery Management units per core, and, until refining moved to the
    /// rank-difference curve, a flat XP per unit from the refinery's ore table (<see cref="LegacyXpPerUnit"/>, ported from commit 64b712fc8
    /// and no longer in the source). The ore list and tiers are still read from the refinery itself.
    /// </summary>
    public static class RefiningModel
    {
        public const int BaseItemsPerCore = 3;
        public const int MaximumRefineryManagement = 6;
        private const int UnavailableLevel = 99;

        /// <summary>RefineryViewModel.cs:168-169.</summary>
        public static int ItemsPerCore(int refineryManagement) => BaseItemsPerCore + refineryManagement;

        public static int CoresRequired(int units, int refineryManagement) =>
            (int)Math.Ceiling(units / (float)ItemsPerCore(refineryManagement));

        /// <summary>Flat XP per unit, whatever the crafter's rank (the removed <c>OreDetail.XPGranted</c>).</summary>
        public static int Xp(OreTier ore, int units) => ore.XpPerUnit * units;

        /// <summary>The flat XP per unit each refined ore paid before refining used the rank-difference curve: 25 to 150 by ore tier.</summary>
        public static readonly IReadOnlyDictionary<string, int> LegacyXpPerUnit = new Dictionary<string, int>
        {
            ["raw_veldite"] = 25, ["ore_tilarium"] = 25,
            ["raw_scordspar"] = 50, ["ore_currian"] = 50,
            ["raw_plagionite"] = 75, ["ore_idailia"] = 75,
            ["raw_keromber"] = 100, ["ore_barinium"] = 100,
            ["raw_jasioclase"] = 125, ["ore_gostian"] = 125,
            ["raw_arkoxit"] = 150, ["ore_arda"] = 150
        };

        /// <summary>The XP per unit refining pays now: <c>GetDeltaXP(ore level - rank)</c>, at the level of the vein of the ore's tier.</summary>
        public static int CurrentXpPerUnit(OreTier ore, int gatheringRank) =>
            ServerXp.ForDelta(SWLOR.Game.Server.Service.CraftService.GatheringLevels.ForTier(ore.RequiredLevel) - gatheringRank);

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
                    LegacyXpPerUnit[(string)entry.Key]));
            }

            return ores.OrderBy(o => o.RequiredLevel).ThenBy(o => o.XpPerUnit).ThenBy(o => o.Ore, StringComparer.Ordinal).ToList();
        }
    }
}
