using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Scenario.Kitchen
{
    /// <summary>
    /// A Chef: the set of Chef perks a Kitchen cook owns. The numbers come from the game's own perk definitions
    /// (<see cref="AgricultureChefPerkDefinition"/>), at the rank the recipe is played at, so a rank-30 Chef owns only the purchases rank
    /// 30 allows and a perk with two ranks gives the higher rank the cook can own. Seasoned Palate is the one choice made for each dish; a
    /// build says whether the cook switches it on.
    /// </summary>
    public sealed class KitchenBuild
    {
        private static readonly Lazy<IReadOnlyDictionary<PerkType, PerkDetail>> Definitions =
            new(() => PerkLines.Build(new AgricultureChefPerkDefinition()));

        private static readonly PerkType[] Styles = { PerkType.SlowCooking, PerkType.FlashCooking };

        private readonly HashSet<PerkType> _perks;

        public KitchenBuild(string name, IEnumerable<PerkType> perks, bool seasonedPalateOn = false)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _perks = new HashSet<PerkType>(perks ?? throw new ArgumentNullException(nameof(perks)));
            SeasonedPalateOn = seasonedPalateOn;
        }

        public string Name { get; }

        /// <summary>True when the cook switches Seasoned Palate on for every dish, if they own it.</summary>
        public bool SeasonedPalateOn { get; }

        /// <summary>The perks this build owns (at the highest rank the cook's rank allows).</summary>
        public IReadOnlyCollection<PerkType> Perks => _perks;

        /// <summary>The perk definitions as the game builds them.</summary>
        public static IReadOnlyDictionary<PerkType, PerkDetail> Line => Definitions.Value;

        /// <summary>Every Chef perk, so a study can build each one alone.</summary>
        public static IReadOnlyCollection<PerkType> AllPerks => Line.Keys.OrderBy(type => (int)type).ToList();

        public static KitchenBuild SlowCookingAlone { get; } = Alone(PerkType.SlowCooking);

        public static KitchenBuild FlashCookingAlone { get; } = Alone(PerkType.FlashCooking);

        public static KitchenBuild SlowCookingLine { get; } = FullLine(PerkType.SlowCooking);

        public static KitchenBuild FlashCookingLine { get; } = FullLine(PerkType.FlashCooking);

        public static KitchenBuild SlowCookingLineSeasoned { get; } = FullLine(PerkType.SlowCooking, seasonedPalateOn: true);

        public static KitchenBuild FlashCookingLineSeasoned { get; } = FullLine(PerkType.FlashCooking, seasonedPalateOn: true);

        /// <summary>A cook who owns one perk only.</summary>
        public static KitchenBuild Alone(PerkType perk, bool seasonedPalateOn = false) =>
            new(Line[perk].Name + (seasonedPalateOn ? " (on)" : string.Empty), new[] { perk }, seasonedPalateOn);

        /// <summary>A cook with the given style and every other perk of the line, named "(full line)".</summary>
        public static KitchenBuild FullLine(PerkType style, bool seasonedPalateOn = false) =>
            new(
                $"{Line[style].Name} (full line{(seasonedPalateOn ? ", Seasoned Palate on" : string.Empty)})",
                Line.Keys.Where(type => type == style || !Styles.Contains(type)),
                seasonedPalateOn);

        public CraftStyle AsStyle() => new(Name, this);

        /// <summary>The numbers the Kitchen reads for a cook of this rank. Below Agriculture 20 there is no style yet.</summary>
        public KitchenPerkStats StatsAt(int rank)
        {
            var totals = PerkLines.TotalsAt(Line, SkillType.Agriculture, rank, _perks.Contains);
            return KitchenPerkStats.FromStatTotals(PerkLines.Lookup(totals), SeasonedPalateOn);
        }
    }
}
