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

        public static IReadOnlyList<CreatureAbilityInfo> Build()
        {
            var result = new Dictionary<int, CreatureAbilityInfo>();
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
                        result[featId] = new CreatureAbilityInfo(
                            featId,
                            string.IsNullOrWhiteSpace(detail.Name) ? Humanize(feat.ToString()) : detail.Name,
                            Describe(detail),
                            perkId,
                            perkName,
                            skillId,
                            skillName,
                            type.Namespace == NpcAbilityNamespace);
                    }
                }
                catch (Exception ex) when (ex is TargetInvocationException or InvalidOperationException or ArgumentException)
                {
                    // One malformed definition must not hide the rest of the registered abilities.
                }
            }

            return result.Values
                .OrderBy(info => info.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(info => info.FeatId)
                .ToList();
        }

        private static string Describe(AbilityDetail detail)
        {
            var parts = new List<string>();
            if (detail.Targeting != null && detail.Targeting.Shape != AbilityTargetingShapeType.None)
            {
                var shape = detail.Targeting.Shape switch
                {
                    AbilityTargetingShapeType.Sphere => $"{detail.Targeting.SizeX:0.##}m radius",
                    AbilityTargetingShapeType.HSphere => $"{detail.Targeting.SizeX:0.##}m radius",
                    AbilityTargetingShapeType.Rect =>
                        $"{detail.Targeting.SizeX:0.##}m × {detail.Targeting.SizeY:0.##}m line",
                    AbilityTargetingShapeType.Cone =>
                        $"{detail.Targeting.SizeX:0.##}m × {detail.Targeting.SizeY:0.##}m cone",
                    _ => Humanize(detail.Targeting.Shape.ToString())
                };
                parts.Add(shape);
            }
            else if (detail.IsSingleTargetAbility || detail.RequiresTarget)
            {
                parts.Add("single target");
            }
            else
            {
                parts.Add("self or passive");
            }

            if (detail.CombatImpactDamageAbility != AbilityType.Invalid)
                parts.Add($"{Humanize(detail.CombatImpactDamageAbility.ToString())} damage scaling");
            if (detail.RecastGroup != RecastGroup.Invalid)
                parts.Add($"{Humanize(detail.RecastGroup.ToString())} recast");
            if (detail.IsHostileAbility)
                parts.Add("hostile");
            if (detail.EffectiveLevelPerkType != PerkType.Invalid)
                parts.Add($"level from {Humanize(detail.EffectiveLevelPerkType.ToString())}");

            return string.Join(" · ", parts);
        }

        private static string Humanize(string value) =>
            System.Text.RegularExpressions.Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2")
                .Replace('_', ' ');
    }
}
