using System;
using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    /// <summary>
    /// The values a weapon ability definition fixes for its hostile impact, passed to the impact part in one value so the call stays short.
    /// </summary>
    public readonly record struct WeaponAbilityImpactRequest(
        WeaponAbilityProfile Profile,
        SkillType Skill,
        int BaseDamage,
        int Duration,
        Type StatusEffect,
        IEnumerable<Type> AdditionalStatusEffects,
        bool IsArea,
        AbilityTargetingShapeType TargetingShape,
        float TargetingSizeX,
        float TargetingSizeY,
        AbilityTargetingFlags TargetingFlags,
        VisualEffect AreaVisualEffect,
        AbilityType CombatImpactDamageAbility,
        Animation ImpactAnimation);
}
