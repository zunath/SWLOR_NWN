using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>
    /// An Angler: the set of Angler perks a fisher owns. The numbers come from the game's own perk definitions
    /// (<see cref="AgricultureAnglerPerkDefinition"/>) at the rank the fight is played at, so a rank-30 Angler owns only the purchases
    /// rank 30 allows and a perk with several ranks gives the highest rank the angler can own, unless the build names a level.
    /// </summary>
    public sealed class AnglerBuild
    {
        private static readonly Lazy<IReadOnlyDictionary<PerkType, PerkDetail>> Definitions =
            new(() => PerkLines.Build(new AgricultureAnglerPerkDefinition()));

        private static readonly PerkType[] Styles = { PerkType.PatientAngler, PerkType.AggressiveAngler };

        private readonly Dictionary<PerkType, int?> _perks;

        /// <param name="name">What the report calls the build.</param>
        /// <param name="perks">Each perk the angler owns, with the level they own it at, or null for the highest level their rank allows.</param>
        public AnglerBuild(string name, IEnumerable<KeyValuePair<PerkType, int?>> perks)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _perks = (perks ?? throw new ArgumentNullException(nameof(perks))).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public string Name { get; }

        /// <summary>The perks this build owns.</summary>
        public IReadOnlyCollection<PerkType> Perks => _perks.Keys;

        /// <summary>The perk definitions as the game builds them.</summary>
        public static IReadOnlyDictionary<PerkType, PerkDetail> Line => Definitions.Value;

        /// <summary>An angler with no perks.</summary>
        public static AnglerBuild Unperked { get; } = new("No perks", Array.Empty<KeyValuePair<PerkType, int?>>());

        /// <summary>Every purchase of the line the rank allows, with Patient Angler as the style.</summary>
        public static AnglerBuild PatientLine { get; } = FullLine(PerkType.PatientAngler);

        /// <summary>Every purchase of the line the rank allows, with Aggressive Angler as the style.</summary>
        public static AnglerBuild AggressiveLine { get; } = FullLine(PerkType.AggressiveAngler);

        /// <summary>The line below the capstone with Patient Angler as the style.</summary>
        public static AnglerBuild PatientBelowCapstone { get; } = FullLine(PerkType.PatientAngler, withTrophyCatch: false);

        /// <summary>The line below the capstone with Aggressive Angler as the style.</summary>
        public static AnglerBuild AggressiveBelowCapstone { get; } = FullLine(PerkType.AggressiveAngler, withTrophyCatch: false);

        /// <summary>An angler who owns one purchase only: the perk at the named level.</summary>
        public static AnglerBuild Alone(PerkType perk, int level) =>
            new($"{Line[perk].Name} {(Line[perk].PerkLevels.Count > 1 ? Roman(level) : string.Empty)}".Trim(), new[] { new KeyValuePair<PerkType, int?>(perk, level) });

        /// <summary>An angler with the given style and every other perk of the line, named "(full line)".</summary>
        public static AnglerBuild FullLine(PerkType style, bool withTrophyCatch = true) =>
            new(
                withTrophyCatch ? $"{Line[style].Name} (full line)" : $"{Line[style].Name} (line below the capstone)",
                Line.Keys
                    .Where(type => type == style || !Styles.Contains(type))
                    .Where(type => withTrophyCatch || type != PerkType.TrophyCatch)
                    .Select(type => new KeyValuePair<PerkType, int?>(type, null)));

        public CraftStyle AsStyle() => new(Name, this);

        /// <summary>The numbers the Line reads for an angler of this rank. Below Agriculture 20 there is no style yet.</summary>
        public LineAnglerEffects EffectsAt(int rank)
        {
            var totals = new Dictionary<StatType, int>();
            foreach (var (type, level) in _perks.OrderBy(pair => (int)pair.Key))
            {
                var owned = level.HasValue ? Line[type].PerkLevels[level.Value] : PerkLines.OwnedLevel(Line[type], SkillType.Agriculture, rank);
                if (owned == null)
                    continue;

                if (level.HasValue && owned.Requirements.OfType<PerkRequirementSkill>().Any(requirement => requirement.RequiredRank > rank))
                    continue;

                foreach (var bonus in owned.StatBonuses)
                {
                    totals.TryGetValue(bonus.Stat, out var current);
                    totals[bonus.Stat] = Stat.AggregateStatAdjustment(bonus.Stat, current, bonus.Calculate(0));
                }
            }

            return LineAnglerEffects.FromStatTotals(PerkLines.Lookup(totals));
        }

        private static string Roman(int level) => level switch { 1 => "I", 2 => "II", 3 => "III", _ => level.ToString() };
    }
}
