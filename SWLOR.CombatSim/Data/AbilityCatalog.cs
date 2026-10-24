using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Reflection;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;

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

            var weapon = WeaponImpactReader.Read(feat, detail);
            return new AbilityInfo
            {
                Feat = feat,
                Technique = detail.MeleeTechnique,
                SourceWeaponPower = detail.MeleeTechnique != null ? ReadTechniquePower(detail) : weapon?.BaseDamage,
                ImpactStatusDuration = detail.MeleeTechnique != null ? ReadImpactCapture<int>(detail, "statusDuration") : weapon?.Duration ?? 0,
                ImpactStatusTypes = weapon?.Statuses ?? (IReadOnlyList<Type>)Array.Empty<Type>(),
                WeaponStatusFactory = weapon?.StatusFactory,
                WeaponImpactSkillType = weapon?.Skill ?? detail.SkillType,
                InterruptsTarget = weapon?.Profile.Read<bool>("ClearTargetActionsOnHit") == true,
                ExtraDamageIfTargetControlled = weapon?.Profile.Read<int>("ExtraDamageIfTargetControlled") ?? 0,
                ConditionalTargetStatus = weapon?.Profile.ReadConditionalTargetStatus(),
                UnmodeledRiders = weapon?.UnmodeledRiders ?? (IReadOnlyList<string>)Array.Empty<string>(),
                RestoresSecondaryTargetStamina = weapon?.RestoresSecondaryTargetStamina == true,
                IsAreaAbility = detail.IsAreaAbility,
                WeaponAreaRadius = detail.Targeting is { Shape: AbilityTargetingShapeType.Sphere } targeting &&
                    targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf) ? targeting.SizeX : 0f,
                MaximumAreaTargets = weapon?.Profile.Read<int>("MaximumAreaTargets") ?? 0,
                ImpactStatusFactory = detail.MeleeTechnique == null ? null : ReadImpactCapture<Func<SWLOR.Game.Server.Service.StatusEffectService.IStatusEffect>>(detail, "statusFactory"),
                Name = detail.Name,
                PerkName = perk?.Name ?? detail.Name,
                RecastGroup = detail.RecastGroup.ToString(),
                SkillRequirement = skillRequirement,
                FP = detail.Requirements.OfType<AbilityRequirementFP>().Sum(r => r.RequiredFP),
                STM = detail.Requirements.OfType<AbilityRequirementStamina>().Sum(r => r.RequiredSTM),
                PaysFpFirst = PaysFpFirst(detail),
                RecastSeconds = recast ?? 0f,
                CastSeconds = cast ?? 0f,
                IsChanneled = detail.IsChanneled,
                ImpactDelaySeconds = detail.ImpactDelay,
                IsQueuedWeapon = detail.ActivationType == AbilityActivationType.Weapon,
                UnreadableTiming = unreadable.Length > 0 ? unreadable : null,
                SkillType = detail.SkillType,
                IsHostile = detail.IsHostileAbility,
                IsAimed = detail.RequiresTarget || detail.RequiresLocationTarget,
                IsHealing = detail.IsHealingAbility,
                ForceAffinity = perk?.ForceAffinityType?.ToString() ?? string.Empty,
                TwinPistol = ReadTwinSpec(detail)
            };
        }

        private static TwinPistolAbilitySpec ReadTwinSpec(AbilityDetail detail)
        {
            if (detail.SkillType != SkillType.TwinPistol || !detail.IsHostileAbility) return null;
            // ConfigurePairAttack captures the authored immutable payload in its impact delegate.
            // Fail closed if the implementation changes; never substitute stale copied numbers.
            var closure = detail.ImpactAction?.Target;
            var payload = closure?.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SingleOrDefault(field => field.FieldType == typeof(TwinPistolAbilitySpec))?.GetValue(closure);
            return payload as TwinPistolAbilitySpec ?? throw new InvalidDataException($"Cannot read the production Twin Pistol payload for {detail.Name}.");
        }

        // The game pays requirements in the order the ability lists them (UsePerkFeat.ApplyRequirementEffects).
        private static bool PaysFpFirst(AbilityDetail detail)
        {
            var requirements = detail.Requirements.ToList();
            var fp = requirements.FindIndex(r => r is AbilityRequirementFP);
            var stamina = requirements.FindIndex(r => r is AbilityRequirementStamina);
            return fp >= 0 && (stamina < 0 || fp < stamina);
        }

        private static T ReadImpactCapture<T>(AbilityDetail detail, string name)
        {
            var closure = detail.ImpactAction?.Target;
            var field = closure?.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (field == null) throw new InvalidDataException($"Cannot read impact metadata {name} for {detail.Name}.");
            return (T)field.GetValue(closure);
        }

        private static int ReadTechniquePower(AbilityDetail detail)
        {
            // ConfigureStrike captures its declared constant power in the impact closure. Fail closed on compiler/layout changes.
            var closure = detail.ImpactAction?.Target;
            var field = closure?.GetType().GetField("power", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (field?.GetValue(closure) is int power) return power;
            throw new InvalidDataException($"Cannot read source power for {detail.Name}; do not substitute scenario damage.");
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
