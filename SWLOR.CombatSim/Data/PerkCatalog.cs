using System.Reflection;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Data
{
    /// <summary>
    /// Reads every perk's levels, granted abilities and stat bonuses from the game server's perk definitions.
    /// </summary>
    public sealed class PerkCatalog
    {
        // Perk stat delegates take an object id; outside the engine any non-zero id works for constant bonuses.
        private const uint PlaceholderObject = 0x7F000000;

        private readonly Dictionary<PerkType, PerkDetail> _perks = new();
        private readonly Dictionary<FeatType, (PerkDetail Perk, int SkillRequirement)> _perksByFeat = new();
        private readonly List<string> _loadErrors = new();

        public IReadOnlyList<string> LoadErrors => _loadErrors;
        public PerkDetail Get(PerkType perk) => _perks.TryGetValue(perk, out var value) ? value
            : throw new InvalidDataException($"Perk {perk} could not be loaded from the game definitions.");
        public IEnumerable<PerkDetail> InCategory(PerkCategoryType category) => _perks.Values.Where(perk => perk.Category == category);

        public static PerkCatalog Load()
        {
            var catalog = new PerkCatalog();
            catalog.LoadAll();
            return catalog;
        }

        public bool TryGetGrantingPerk(FeatType feat, out PerkDetail perk, out int skillRequirement)
        {
            var found = _perksByFeat.TryGetValue(feat, out var entry);
            perk = entry.Perk;
            skillRequirement = entry.SkillRequirement;
            return found;
        }

        /// <summary>
        /// The listed perks' combined bonuses to <paramref name="stats"/>, the way the game applies them: each perk's
        /// perk-wide bonuses plus those of its chosen level. A perk is written as its PerkType name, with ":level"
        /// when it has more than one level.
        /// </summary>
        public Dictionary<StatType, int> GetStats(IEnumerable<string> perks, IReadOnlyCollection<StatType> stats)
        {
            var totals = new Dictionary<StatType, int>();
            var owned = new HashSet<PerkType>();
            foreach (var entry in perks ?? Enumerable.Empty<string>())
            {
                var (perk, level) = Parse(entry);
                // A character owns each perk once, at one level.
                if (!owned.Add(perk.Type))
                    throw new InvalidDataException($"Perk '{perk.Type}' is listed more than once.");
                foreach (var bonus in perk.StatBonuses.Concat(level.StatBonuses).Where(b => stats.Contains(b.Stat)))
                {
                    int value;
                    try
                    {
                        value = bonus.Calculate(PlaceholderObject);
                    }
                    catch (Exception)
                    {
                        throw new InvalidDataException($"Perk '{entry}' computes {bonus.Stat} from the game engine, which the simulator cannot read.");
                    }

                    totals.TryGetValue(bonus.Stat, out var current);
                    totals[bonus.Stat] = Stat.AggregateStatAdjustment(bonus.Stat, current, value);
                }
            }

            return totals;
        }

        public int GetPurchaseCost(IEnumerable<string> entries) => entries.Select(Parse)
            .Sum(pair => pair.Perk.PerkLevels.Where(level => level.Key <= pair.Perk.PerkLevels.Single(level => ReferenceEquals(level.Value, pair.Level)).Key)
                .Sum(level => level.Value.Price));

        public void ValidateWeaponBuild(IEnumerable<string> entries, IEnumerable<FeatType> abilities, int skillRank,
            IReadOnlyCollection<PerkType> modeledActivePerks = null)
        {
            var parsed = entries.Select(Parse).ToList();
            var owned = parsed.Select(pair => pair.Perk.Type).ToHashSet();
            foreach (var pair in parsed)
            {
                if (pair.Perk.Category is not (PerkCategoryType.TwinPistolBarrage or PerkCategoryType.TwinPistolCrossfire) &&
                    !(modeledActivePerks?.Contains(pair.Perk.Type) == true &&
                      !pair.Perk.StatBonuses.Any() && !pair.Level.StatBonuses.Any()))
                    throw new InvalidDataException($"The stat-based weapon model does not implement {pair.Perk.Type}; use native comparison coverage for other perk lines.");
                if (pair.Level.Requirements.OfType<PerkRequirementSkill>().Any(requirement => requirement.RequiredRank > skillRank))
                    throw new InvalidDataException($"Perk {pair.Perk.Type} requires a higher skill rank.");
                foreach (var exclusion in pair.Level.Requirements.OfType<PerkRequirementCannotHavePerk>())
                {
                    var excluded = (PerkType)typeof(PerkRequirementCannotHavePerk)
                        .GetField("_cannotHavePerkType", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(exclusion)!;
                    if (owned.Contains(excluded)) throw new InvalidDataException($"Perks {pair.Perk.Type} and {excluded} are mutually exclusive.");
                }
                foreach (var requirement in pair.Level.Requirements.OfType<PerkRequirementMustHavePerk>())
                {
                    var type = typeof(PerkRequirementMustHavePerk);
                    var requiredPerk = (PerkType)type.GetField("_mustHavePerkType", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(requirement)!;
                    var requiredLevel = (int)type.GetField("_mustHavePerkLevel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(requirement)!;
                    var purchased = parsed.FirstOrDefault(candidate => candidate.Perk.Type == requiredPerk);
                    if (purchased.Perk == null || !purchased.Perk.PerkLevels.Any(level => ReferenceEquals(level.Value, purchased.Level) && level.Key >= requiredLevel))
                        throw new InvalidDataException($"Perk {pair.Perk.Type} requires {requiredPerk}:{requiredLevel}.");
                }
            }
            var granted = parsed.SelectMany(pair => pair.Level.GrantedFeats).ToHashSet();
            foreach (var feat in abilities)
                if (!granted.Contains(feat)) throw new InvalidDataException($"Weapon build has not purchased {feat}.");
        }

        private (PerkDetail Perk, PerkLevel Level) Parse(string entry)
        {
            var parts = (entry ?? string.Empty).Split(':');
            if (parts.Length > 2 || !Enum.TryParse<PerkType>(parts[0].Trim(), true, out var perkType) || !Enum.IsDefined(perkType))
                throw new InvalidDataException($"Unknown perk '{entry}'. Use a PerkType name, with ':level' when the perk has several levels.");

            if (!_perks.TryGetValue(perkType, out var perk))
                throw new InvalidDataException($"Perk '{entry}' could not be read from the game server's perk definitions.");

            int levelNumber;
            if (parts.Length == 1)
            {
                if (perk.PerkLevels.Count != 1)
                    throw new InvalidDataException($"Perk '{entry}' has {perk.PerkLevels.Count} levels; write it as '{perkType}:level'.");

                levelNumber = perk.PerkLevels.Keys.Single();
            }
            else if (!int.TryParse(parts[1].Trim(), out levelNumber) || !perk.PerkLevels.ContainsKey(levelNumber))
            {
                throw new InvalidDataException($"Perk '{entry}' names a level the perk does not have.");
            }

            return (perk, perk.PerkLevels[levelNumber]);
        }

        private void LoadAll()
        {
            var assembly = typeof(IPerkListDefinition).Assembly;
            foreach (var type in assembly.GetTypes()
                         .Where(t => typeof(IPerkListDefinition).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
            {
                var perks = BuildPerks(type);
                if (perks == null)
                    continue;

                foreach (var (perkType, perk) in perks)
                {
                    _perks.TryAdd(perkType, perk);
                    foreach (var (_, level) in perk.PerkLevels)
                    {
                        var skill = level.Requirements.OfType<PerkRequirementSkill>()
                            .Select(r => r.RequiredRank)
                            .DefaultIfEmpty(0)
                            .Max();
                        foreach (var feat in level.GrantedFeats)
                            _perksByFeat.TryAdd(feat, (perk, skill));
                    }
                }
            }
        }

        // Some perk definitions call engine functions while building; build them method by method
        // and keep whatever was registered before the failure.
        private Dictionary<PerkType, PerkDetail> BuildPerks(Type type)
        {
            try
            {
                return ((IPerkListDefinition)Activator.CreateInstance(type)).BuildPerks();
            }
            catch (Exception)
            {
                var instance = Activator.CreateInstance(type);
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                             .Where(m => m.ReturnType == typeof(void) && m.GetParameters().Length == 0 && !m.Name.Contains('<'))
                             .OrderBy(m => m.MetadataToken))
                {
                    try
                    {
                        method.Invoke(instance, null);
                    }
                    catch (Exception ex)
                    {
                        _loadErrors.Add($"{type.Name}.{method.Name}: {ex.InnerException?.GetType().Name}");
                    }
                }

                var builder = type.GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance);
                if (builder == null)
                {
                    _loadErrors.Add($"{type.Name}: no perk builder");
                    return null;
                }

                return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
                    .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)?
                    .GetValue(builder);
            }
        }
    }
}
