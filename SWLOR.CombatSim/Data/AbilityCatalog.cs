using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Data
{
    /// <summary>
    /// Reads every ability's cost, recast and cast time from the game server's ability and perk
    /// definitions, so the simulator always runs on the current data.
    /// </summary>
    public sealed class AbilityCatalog
    {
        // Ability delegates take an object id; outside the engine any non-zero id works for data-only lambdas.
        private const uint PlaceholderObject = 0x7F000000;

        private readonly Dictionary<FeatType, AbilityInfo> _abilities = new();
        private readonly List<string> _loadErrors = new();

        private AbilityCatalog(PerkCatalog perks)
        {
            Perks = perks;
            _loadErrors.AddRange(perks.LoadErrors);
        }

        public IReadOnlyDictionary<FeatType, AbilityInfo> Abilities => _abilities;
        public IReadOnlyList<string> LoadErrors => _loadErrors;
        public PerkCatalog Perks { get; }

        public static AbilityCatalog Load()
        {
            var catalog = new AbilityCatalog(PerkCatalog.Load());
            catalog.LoadAll();
            return catalog;
        }

        public AbilityInfo Get(FeatType feat)
        {
            if (!_abilities.TryGetValue(feat, out var info))
                throw new KeyNotFoundException($"Ability '{feat}' was not found in the game server's ability definitions.");

            return info;
        }

        private void LoadAll()
        {
            var assembly = typeof(IAbilityListDefinition).Assembly;

            foreach (var type in assembly.GetTypes()
                         .Where(t => typeof(IAbilityListDefinition).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                         .OrderBy(t => t.FullName))
            {
                Dictionary<FeatType, AbilityDetail> abilities;
                try
                {
                    abilities = ((IAbilityListDefinition)Activator.CreateInstance(type)).BuildAbilities();
                }
                catch (Exception ex)
                {
                    _loadErrors.Add($"{type.Name}: {ex.GetType().Name}");
                    continue;
                }

                foreach (var (feat, detail) in abilities)
                    Add(Describe(feat, detail));
            }
        }

        internal void Add(AbilityInfo info) => _abilities[info.Feat] = info;

        /// <summary>Reads one ability rank; a recast or cast time the game computes from engine state is flagged, never read as zero.</summary>
        internal AbilityInfo Describe(FeatType feat, AbilityDetail detail)
        {
            Perks.TryGetGrantingPerk(feat, out var perk, out var skillRequirement);
            var recast = TryEvaluate(() => detail.RecastDelay?.Invoke(PlaceholderObject) ?? 0f);
            var cast = TryEvaluate(() => detail.ActivationDelay?.Invoke(PlaceholderObject, PlaceholderObject, detail.AbilityLevel) ?? 0f);
            var unreadable = string.Join(" and ", new[] { recast.HasValue ? null : "recast", cast.HasValue ? null : "cast time" }.Where(t => t != null));
            if (unreadable.Length > 0)
                _loadErrors.Add($"{feat}: {unreadable} needs the game engine");

            return new AbilityInfo
            {
                Feat = feat,
                Name = detail.Name,
                PerkName = perk?.Name ?? detail.Name,
                RecastGroup = detail.RecastGroup.ToString(),
                SkillRequirement = skillRequirement,
                FP = detail.Requirements.OfType<AbilityRequirementFP>().Sum(r => r.RequiredFP),
                STM = detail.Requirements.OfType<AbilityRequirementStamina>().Sum(r => r.RequiredSTM),
                RecastSeconds = recast ?? 0f,
                CastSeconds = cast ?? 0f,
                IsQueuedWeapon = detail.ActivationType == AbilityActivationType.Weapon,
                UnreadableTiming = unreadable.Length > 0 ? unreadable : null,
                SkillType = detail.SkillType,
                IsHostile = detail.IsHostileAbility,
                IsHealing = detail.IsHealingAbility,
                ForceAffinity = perk?.ForceAffinityType?.ToString() ?? string.Empty
            };
        }

        private static float? TryEvaluate(Func<float> evaluate)
        {
            try
            {
                return evaluate();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
