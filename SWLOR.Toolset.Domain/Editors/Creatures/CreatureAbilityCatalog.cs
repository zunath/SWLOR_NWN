using System.Reflection;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Toolset.Domain.Editors.Creatures
{
    /// <summary>Registered abilities projected without initializing the live game cache.</summary>
    public static class CreatureAbilityCatalog
    {
        private const string NpcAbilityNamespace = "SWLOR.Game.Server.Feature.AbilityDefinition.NPC";

        public static IReadOnlyList<CreatureAbilityInfo> Build(
            IReadOnlyDictionary<int, CreaturePerkInfo>? perks = null)
        {
            perks ??= CreaturePerkCatalog.Build();
            var descriptions = perks.Values
                .SelectMany(perk => perk.GrantedFeatDescriptions ?? new Dictionary<int, string>())
                .GroupBy(pair => pair.Key)
                .ToDictionary(group => group.Key, group => group.Last().Value);
            var definitions = new Dictionary<int, (AbilityDetail Detail, Type Type, string Name)>();
            var assembly = typeof(IAbilityListDefinition).Assembly;
            foreach (var type in assembly.GetTypes()
                         .Where(type => typeof(IAbilityListDefinition).IsAssignableFrom(type) &&
                                        !type.IsAbstract && !type.IsInterface))
            {
                try
                {
                    if (Activator.CreateInstance(type) is not IAbilityListDefinition definition)
                        continue;

                    foreach (var (feat, detail) in definition.BuildAbilities())
                    {
                        var featId = Convert.ToInt32(feat);
                        definitions[featId] = (
                            detail,
                            type,
                            string.IsNullOrWhiteSpace(detail.Name) ? Humanize(feat.ToString()) : detail.Name);
                    }
                }
                catch (Exception ex) when (ex is TargetInvocationException or InvalidOperationException or ArgumentException)
                {
                    // One malformed definition must not hide the rest of the registered abilities.
                }
            }

            // NPC abilities are separate feats that mimic player abilities. Reuse the player-facing
            // perk description for those NPC feats instead of showing implementation metadata.
            foreach (var (featId, definition) in definitions)
            {
                if (definition.Detail.MimicrySourceFeat == FeatType.Invalid ||
                    !descriptions.TryGetValue(
                        Convert.ToInt32(definition.Detail.MimicrySourceFeat),
                        out var description))
                    continue;

                descriptions.TryAdd(featId, description);
            }

            return definitions.Select(pair =>
                {
                    var (featId, definition) = pair;
                    var detail = definition.Detail;
                    var perkId = Convert.ToInt32(detail.EffectiveLevelPerkType);
                    var perkName = detail.EffectiveLevelPerkType == PerkType.Invalid
                        ? string.Empty
                        : Humanize(detail.EffectiveLevelPerkType.ToString());
                    var skillId = Convert.ToInt32(detail.SkillType);
                    var skillName = detail.SkillType == SkillType.Invalid
                        ? string.Empty
                        : typeof(SkillType).GetField(detail.SkillType.ToString())?
                            .GetCustomAttribute<SkillAttribute>()?.Name ??
                          Humanize(detail.SkillType.ToString());
                    return new CreatureAbilityInfo(
                        featId,
                        definition.Name,
                        descriptions.TryGetValue(featId, out var description)
                            ? Concise(description)
                            : DescribeFallback(detail),
                        perkId,
                        perkName,
                        skillId,
                        skillName,
                        definition.Type.Namespace == NpcAbilityNamespace);
                })
                .OrderBy(info => info.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(info => info.FeatId)
                .ToList();
        }

        private static string DescribeFallback(AbilityDetail detail)
        {
            string scope;
            if (detail.Targeting != null && detail.Targeting.Shape != AbilityTargetingShapeType.None)
            {
                scope = detail.Targeting.Shape switch
                {
                    AbilityTargetingShapeType.Sphere =>
                        $"Affects enemies in a {detail.Targeting.SizeX:0.##}m radius.",
                    AbilityTargetingShapeType.HSphere =>
                        $"Affects enemies in a {detail.Targeting.SizeX:0.##}m radius.",
                    AbilityTargetingShapeType.Rect =>
                        $"Hits enemies in a {detail.Targeting.SizeX:0.##}m \u00d7 {detail.Targeting.SizeY:0.##}m line.",
                    AbilityTargetingShapeType.Cone =>
                        $"Hits enemies in a {detail.Targeting.SizeX:0.##}m \u00d7 {detail.Targeting.SizeY:0.##}m cone.",
                    _ => $"Uses a {Humanize(detail.Targeting.Shape.ToString()).ToLowerInvariant()} area."
                };
            }
            else if (detail.IsSingleTargetAbility || detail.RequiresTarget)
            {
                scope = detail.IsHostileAbility ? "Hits one target." : "Affects one target.";
            }
            else
            {
                scope = "Affects the creature itself or grants a passive effect.";
            }

            if (detail.CombatImpactDamageAbility == AbilityType.Invalid)
                return scope;

            return $"{scope} Damage scales with " +
                   $"{Humanize(detail.CombatImpactDamageAbility.ToString())}.";
        }

        private static string Concise(string description)
        {
            const int maximumLength = 180;
            var normalized = System.Text.RegularExpressions.Regex.Replace(description, "\\s+", " ").Trim();
            if (normalized.Length <= maximumLength)
                return normalized;

            var sentenceEnd = normalized.LastIndexOfAny(['.', '!', '?'], maximumLength - 1);
            if (sentenceEnd >= maximumLength / 2)
                return normalized[..(sentenceEnd + 1)];

            var wordEnd = normalized.LastIndexOf(' ', maximumLength - 1);
            return $"{normalized[..Math.Max(1, wordEnd)]}\u2026";
        }

        private static string Humanize(string value) =>
            System.Text.RegularExpressions.Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2")
                .Replace('_', ' ');
    }
}
