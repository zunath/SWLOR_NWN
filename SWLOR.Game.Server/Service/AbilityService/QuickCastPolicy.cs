using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AbilityService;

public static class QuickCastPolicy
{
    public static bool BlocksCasting(EffectTypeScript effectType) => effectType is
        EffectTypeScript.Paralyze or EffectTypeScript.CutsceneParalyze or
        EffectTypeScript.Dazed or EffectTypeScript.Stunned or EffectTypeScript.Sleep or
        EffectTypeScript.Petrify or EffectTypeScript.Knockdown or
        EffectTypeScript.Confused or EffectTypeScript.Frightened or
        EffectTypeScript.Dominated or EffectTypeScript.CutsceneDominated or EffectTypeScript.Turned;

    public static bool IsEligible(AbilityDetail ability, QuickCastTargetType targetType)
    {
        if (ability == null ||
            ability.ActivationType != AbilityActivationType.Casted ||
            !ability.RequiresTarget ||
            ability.UsesActiveAttackTarget)
        {
            return false;
        }

        if (ability.IsAreaAbility)
        {
            return targetType == QuickCastTargetType.Self &&
                   !ability.IsHostileAbility && ability.AllowsSelfQuickCast &&
                   ability.Targeting is
                   {
                       Shape: AbilityTargetingShapeType.Sphere,
                       Flags: AbilityTargetingFlags.HelpsAllies
                   };
        }

        if (!ability.IsSingleTargetAbility ||
            ability.Targeting is { Shape: not AbilityTargetingShapeType.None })
            return false;

        return targetType switch
        {
            QuickCastTargetType.Self => !ability.IsHostileAbility && ability.AllowsSelfQuickCast,
            QuickCastTargetType.CurrentEnemy => ability.IsHostileAbility,
            _ => false
        };
    }
}
