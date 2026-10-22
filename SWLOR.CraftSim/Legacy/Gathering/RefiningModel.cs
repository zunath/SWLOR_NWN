using System.Collections;
using System.Reflection;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.CraftSim.Legacy.Gathering
{
    /// <summary>Refining ore with power cores (RefineryViewModel.cs): 3 + Refinery Management units per core, flat XP per unit from the refinery's ore table.</summary>
    public static class RefiningModel
    {
        public const int BaseItemsPerCore = 3;
        public const int MaximumRefineryManagement = 6;
        private const int UnavailableLevel = 99;

        /// <summary>RefineryViewModel.cs:169-171.</summary>
        public static int ItemsPerCore(int refineryManagement) => BaseItemsPerCore + refineryManagement;

        public static int CoresRequired(int units, int refineryManagement) =>
            (int)Math.Ceiling(units / (float)ItemsPerCore(refineryManagement));

        /// <summary>RefineryViewModel.cs:348: flat XP per unit, whatever the crafter's rank.</summary>
        public static int Xp(OreTier ore, int units) => ore.XpPerUnit * units;

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
