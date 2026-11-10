using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.CraftSim.Data
{
    /// <summary>
    /// One enhancement item blueprint: what kind of recipe it enhances, its level, and the progress penalty it adds to a craft.
    /// All three are item properties in Module/uti, read as <c>CraftEnhancementReader</c> reads them when a player slots the item.
    /// </summary>
    public sealed class EnhancementItem
    {
        public EnhancementItem(string resref, string name, RecipeEnhancementType type, int level, int progressPenalty)
        {
            Resref = resref;
            Name = name;
            Type = type;
            Level = level;
            ProgressPenalty = progressPenalty;
        }

        public string Resref { get; }
        public string Name { get; }

        /// <summary>The kind of recipe that accepts it: armor, weapon, structure, food, starship, module or droid.</summary>
        public RecipeEnhancementType Type { get; }

        /// <summary>The item's <c>EnhancementLevel</c>; a recipe accepts it when this is at most five above the recipe's level.</summary>
        public int Level { get; }

        /// <summary>The sum of the item's <c>ProgressPenalty</c> cost-table values: the progress it adds to the piece.</summary>
        public int ProgressPenalty { get; }

        /// <summary>
        /// The tier the items of a level are named by: 5 is I, 15 is II, 25 is III, 35 is IV, 45 is V and the few level 50 droid items VI.
        /// </summary>
        public int Tier => TierOf(Level);

        public static int TierOf(int level) => level / 10 + 1;

        public static string TierName(int tier) => tier switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            6 => "VI",
            _ => tier.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        public override string ToString() => $"{Name} ({Resref}, level {Level}, penalty {ProgressPenalty})";
    }
}
