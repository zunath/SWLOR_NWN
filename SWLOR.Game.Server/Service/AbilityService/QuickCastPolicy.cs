using System.Collections.Generic;
using System.Linq;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AbilityService;

public static class QuickCastPolicy
{
    public static bool IsEligible(AbilityDetail ability, QuickCastTargetType targetType)
    {
        if (ability == null ||
            ability.ActivationType != AbilityActivationType.Casted ||
            !ability.IsSingleTargetAbility ||
            ability.IsAreaAbility ||
            !ability.RequiresTarget ||
            ability.UsesActiveAttackTarget ||
            ability.Targeting is { Shape: not AbilityTargetingShapeType.None })
        {
            return false;
        }

        return targetType switch
        {
            QuickCastTargetType.Self => !ability.IsHostileAbility && ability.AllowsSelfQuickCast,
            QuickCastTargetType.CurrentEnemy => ability.IsHostileAbility,
            _ => false
        };
    }

    public static bool TryFindAbility(
        IReadOnlyDictionary<FeatType, AbilityDetail> abilities,
        string nameOrId,
        QuickCastTargetType targetType,
        Func<FeatType, bool> knowsFeat,
        out FeatType feat)
    {
        feat = FeatType.Invalid;
        var input = nameOrId?.Trim();
        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (int.TryParse(input, out var featId))
        {
            var candidate = (FeatType)featId;
            if (!abilities.TryGetValue(candidate, out var ability) ||
                !IsEligible(ability, targetType) || !knowsFeat(candidate))
                return false;

            feat = candidate;
            return true;
        }

        var matches = abilities.Where(entry =>
                IsEligible(entry.Value, targetType) && knowsFeat(entry.Key) &&
                (input.Equals(entry.Value.Name, StringComparison.OrdinalIgnoreCase) ||
                 input.Equals(entry.Key.ToString(), StringComparison.OrdinalIgnoreCase)))
            .Select(entry => entry.Key)
            .ToArray();
        if (matches.Length != 1)
            return false;

        feat = matches[0];
        return true;
    }
}
