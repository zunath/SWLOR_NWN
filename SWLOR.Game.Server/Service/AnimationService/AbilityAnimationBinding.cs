using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AnimationService;

/// <summary>Connects catalog motions to exact feat ranks without changing ability mechanics.</summary>
public static class AbilityAnimationBinding
{
    public static AnimationClip ActivationClip(AbilityDetail ability, bool isPlayer, float? animationWindow = null, bool equipmentCompatible = true) =>
        !equipmentCompatible || ability.HasGeneratedAnimationBinding && (!isPlayer || !ability.UsesImmediateAuthoredAnimation && animationWindow.HasValue &&
            (ability.AuthoredAnimation == null || ability.AuthoredAnimation.Duration > animationWindow.Value))
            ? null : ability.AuthoredAnimation;

    public static AnimationClip ImpactClip(AbilityDetail ability, bool isPlayer, bool equipmentCompatible = true) =>
        isPlayer && equipmentCompatible ? ability?.AuthoredImpactAnimation : null;

    public static AnimationClip QueuedClip(AbilityDetail ability, bool isPlayer, bool equipmentCompatible = true) =>
        !equipmentCompatible || ability.HasGeneratedAnimationBinding && !isPlayer ? null : ability.QueuedAttackAnimation;

    public static Animation ActivationType(AbilityDetail ability, bool isPlayer, float? animationWindow = null, bool equipmentCompatible = true) =>
        !equipmentCompatible && !ability.HasGeneratedAnimationBinding && ability.AuthoredAnimation != null
            ? Animation.Invalid
            : ability.HasGeneratedAnimationBinding && ActivationClip(ability, isPlayer, animationWindow, equipmentCompatible) == null
                ? ability.NativeAnimationType : ability.AnimationType;

    // Gameplay callers resolve equipment before installing a gesture or queued swing replacement.
    public static AnimationClip ActivationClip(AbilityDetail ability, uint creature, float? animationWindow = null) =>
        ActivationClip(ability, GetIsPC(creature), animationWindow, IsEquipmentCompatible(ability, creature));

    public static AnimationClip ImpactClip(AbilityDetail ability, uint creature) =>
        ImpactClip(ability, GetIsPC(creature), IsEquipmentCompatible(ability, creature));

    public static AnimationClip QueuedClip(AbilityDetail ability, uint creature) =>
        QueuedClip(ability, GetIsPC(creature), IsEquipmentCompatible(ability, creature));

    public static Animation ActivationType(AbilityDetail ability, uint creature, float? animationWindow = null) =>
        ActivationType(ability, GetIsPC(creature), animationWindow, IsEquipmentCompatible(ability, creature));

    private static bool IsEquipmentCompatible(AbilityDetail ability, uint creature) =>
        AbilityAnimationEquipment.IsCompatible(ability?.AnimationEquipmentRequirement ?? AnimationEquipmentRequirement.Unrestricted, creature);

    public static void Apply(IReadOnlyDictionary<FeatType, AbilityDetail> abilities,
        IEnumerable<AbilityAnimationEntry> entries)
    {
        var assigned = new HashSet<FeatType>();
        foreach (var entry in entries)
        foreach (var feat in entry.Feats)
        {
            if (!assigned.Add(feat)) throw new InvalidOperationException($"Duplicate animation binding for {feat}.");
            if (!abilities.TryGetValue(feat, out var ability))
                throw new InvalidOperationException($"Animation {entry.Id} references missing ability {feat}.");
            if (ability.IsMimicryTrait)
                throw new InvalidOperationException($"Animation {entry.Id} references passive trait {feat}.");
            ability.PreviewAnimation = entry.Clip;
            ability.AnimationEquipmentRequirement = entry.EquipmentRequirement;
            if (ability.UsesImmediateAuthoredAnimation || ability.UsesAuthoredImpactAnimation)
            {
                if (ability.UsesImmediateAuthoredAnimation && ability.UsesAuthoredImpactAnimation ||
                    ability.ActivationType == AbilityActivationType.Weapon || ability.IsChanneled)
                    throw new InvalidOperationException($"Invalid immediate animation stage for {feat}.");
                if (!ability.HasGeneratedAnimationBinding)
                    ability.NativeAnimationType = ability.AnimationType;
                ability.HasGeneratedAnimationBinding = true;
                if (ability.UsesAuthoredImpactAnimation)
                    ability.AuthoredImpactAnimation = entry.Clip;
                else
                {
                    ability.AuthoredAnimation = entry.Clip;
                    ability.AnimationType = Animation.PointForward;
                }
                continue;
            }
            // Explicit authored bindings remain authoritative. Native replacements carry projectile,
            // equipment and channel semantics which an ordinary pose must not overwrite.
            if (ability.AuthoredAnimation != null || ability.QueuedAttackAnimation != null ||
                ability.IsChanneled || ability.CanBeUsedInSpace || ability.PreservesStealthDuringActivation ||
                ability.PreservesNativeAnimationChoreography ||
                ability.AnimationType == Animation.ThrowGrenade ||
                ability.SkillType is SkillType.Pistol or SkillType.Rifle or SkillType.Throwing ||
                !string.IsNullOrEmpty(ability.AnimationReplacementAnimationName) ||
                ability.ActivationType != AbilityActivationType.Weapon &&
                (ability.ImpactAnimationType != Animation.Invalid ||
                 !string.IsNullOrEmpty(ability.ImpactAnimationReplacementAnimationName))) continue;
            if (ability.ActivationType == AbilityActivationType.Weapon)
            {
                // QueuedAttackAnimation owns melee swing keys only. Native ranged fire/throw and
                // projectile release remain untouched, and no additional attack is enqueued.
                ability.QueuedAttackAnimation = entry.Clip;
                ability.HasGeneratedAnimationBinding = true;
            }
            else if (ability.ActivationType == AbilityActivationType.Casted)
            {
                ability.AuthoredAnimation = entry.Clip;
                ability.NativeAnimationType = ability.AnimationType;
                ability.HasGeneratedAnimationBinding = true;
                ability.AnimationType = Animation.PointForward;
            }
        }
    }
}
