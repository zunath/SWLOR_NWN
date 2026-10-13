using System.Reflection;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
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

        public IReadOnlyDictionary<FeatType, AbilityInfo> Abilities => _abilities;
        public IReadOnlyList<string> LoadErrors => _loadErrors;

        public static AbilityCatalog Load()
        {
            var catalog = new AbilityCatalog();
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
            var perksByFeat = LoadPerkData(assembly);

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
                {
                    perksByFeat.TryGetValue(feat, out var perk);
                    _abilities[feat] = new AbilityInfo
                    {
                        Feat = feat,
                        Name = detail.Name,
                        PerkName = perk.Name ?? detail.Name,
                        RecastGroup = detail.RecastGroup.ToString(),
                        SkillRequirement = perk.SkillRequirement,
                        FP = detail.Requirements.OfType<AbilityRequirementFP>().Sum(r => r.RequiredFP),
                        STM = detail.Requirements.OfType<AbilityRequirementStamina>().Sum(r => r.RequiredSTM),
                        RecastSeconds = TryEvaluate(() => detail.RecastDelay?.Invoke(PlaceholderObject) ?? 0f),
                        CastSeconds = TryEvaluate(() => detail.ActivationDelay?.Invoke(PlaceholderObject, PlaceholderObject, detail.AbilityLevel) ?? 0f),
                        IsHostile = detail.IsHostileAbility,
                        IsHealing = detail.IsHealingAbility,
                        ForceAffinity = perk.ForceAffinity ?? string.Empty
                    };
                }
            }
        }

        private Dictionary<FeatType, (string Name, int SkillRequirement, string ForceAffinity)> LoadPerkData(Assembly assembly)
        {
            var result = new Dictionary<FeatType, (string, int, string)>();
            foreach (var type in assembly.GetTypes()
                         .Where(t => typeof(IPerkListDefinition).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
            {
                var perks = BuildPerks(type);
                if (perks == null)
                    continue;

                foreach (var (_, perk) in perks)
                {
                    foreach (var (_, level) in perk.PerkLevels)
                    {
                        var skill = level.Requirements.OfType<PerkRequirementSkill>()
                            .Select(r => r.RequiredRank)
                            .DefaultIfEmpty(0)
                            .Max();
                        foreach (var feat in level.GrantedFeats)
                            result.TryAdd(feat, (perk.Name, skill, perk.ForceAffinityType?.ToString()));
                    }
                }
            }

            return result;
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

        private static float TryEvaluate(Func<float> evaluate)
        {
            try
            {
                return evaluate();
            }
            catch (Exception)
            {
                return 0f;
            }
        }
    }
}
