using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class ForceStunStatusEffectDefinition: IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            ForceStun1(builder);
            ForceStun2(builder);
            ForceStun3(builder);

            return builder.Build();
        }

        private void ForceStun1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceStun1)
                .Name("Force Stun I")
                .EffectIcon(17) // 17 = Dazed
                .GrantAction((source, target, length) =>
                {
                    if (!Ability.GetAbilityResisted(source, target, AbilityType.Intelligence, AbilityType.Wisdom))
                    {
                        var effect = EffectDazed();
                        effect = EffectLinkEffects(effect, EffectVisualEffect(VisualEffect.Vfx_Dur_Iounstone_Blue));
                        effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.ForceStun1);
                        ApplyEffectToObject(DurationType.Permanent, effect, target);
                    }
                    else
                    {
                        var effect = EffectAttackDecrease(5);
                        effect = EffectLinkEffects(effect, EffectACDecrease(5));
                        effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.ForceStun1);
                        ApplyEffectToObject(DurationType.Permanent, effect, target);
                    }
                    CombatPoint.AddCombatPointToAllTagged(target, SkillType.Force, 3);
                })
                .RemoveAction((target) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.ForceStun1);
                });
        }
        private void ForceStun2(StatusEffectBuilder builder)
        {            
            builder.Create(StatusEffectType.ForceStun2)
                .Name("Force Stun II")
                .EffectIcon(17) // 17 = Dazed
                .GrantAction((source, target, length) =>
                {
                    const float radiusSize = RadiusSize.Medium;
                    if (!Ability.GetAbilityResisted(source, target, AbilityType.Intelligence, AbilityType.Wisdom))
                    {
                        var effect = EffectDazed();
                        effect = EffectLinkEffects(effect, EffectVisualEffect(VisualEffect.Vfx_Dur_Iounstone_Blue));
                        effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.ForceStun2);
                        ApplyEffectToObject(DurationType.Permanent, effect, target);
                    }
                    else
                    {
                        var effect = EffectAttackDecrease(5);
                        effect = EffectLinkEffects(effect, EffectACDecrease(5));
                        effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.ForceStun2);
                        ApplyEffectToObject(DurationType.Permanent, effect, target);
                    }

                    // Target the next nearest creature and do the same thing.
                    var targetCreature = GetFirstObjectInShape(Shape.Sphere, radiusSize, GetLocation(target), true);
                    while (GetIsObjectValid(targetCreature))
                    {
                        if (targetCreature != target && GetIsReactionTypeHostile(target, source))
                        {
                            // Apply to nearest other creature, then exit loop. 
                            // Intentionally applying Force Stun I so that it doesn't continue to chain exponentially.
                            StatusEffect.Apply(source, target, StatusEffectType.ForceStun1, 0f);
                            break;
                        }
                        targetCreature = GetNextObjectInShape(Shape.Sphere, radiusSize, GetLocation(target), true);
                    }
                    CombatPoint.AddCombatPointToAllTagged(target, SkillType.Force, 3);
                })
                .RemoveAction((target) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.ForceStun2);
                });
        }
        private void ForceStun3(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceStun3)
                .Name("Force Stun III")
                .EffectIcon(17) // 17 = Dazed
                .GrantAction((source, target, length) =>
                {
                    const float radiusSize = RadiusSize.Medium;
                    if (!Ability.GetAbilityResisted(source, target, AbilityType.Intelligence, AbilityType.Wisdom))
                    {
                        var effect = EffectDazed();
                        effect = EffectLinkEffects(effect, EffectVisualEffect(VisualEffect.Vfx_Dur_Iounstone_Blue));
                        effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.ForceStun3);
                        ApplyEffectToObject(DurationType.Permanent, effect, target);
                    }
                    else
                    {
                        var effect = EffectAttackDecrease(5);
                        effect = EffectLinkEffects(effect, EffectACDecrease(5));
                        effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.ForceStun3);
                        ApplyEffectToObject(DurationType.Permanent, effect, target);
                    }

                    // Target the next nearest creature and do the same thing.
                    var targetCreature = GetFirstObjectInShape(Shape.Sphere, radiusSize, GetLocation(target), true);
                    while (GetIsObjectValid(targetCreature))
                    {
                        if (targetCreature != target && GetIsReactionTypeHostile(target, source))
                        {
                            // Apply to nearest other creature, then move on to the next.
                            // Intentionally applying Force Stun I so that it doesn't continue to chain exponentially.
                            StatusEffect.Apply(source, target, StatusEffectType.ForceStun1, 0f);
                        }
                        targetCreature = GetNextObjectInShape(Shape.Sphere, radiusSize, GetLocation(target), true);
                    }
                    CombatPoint.AddCombatPointToAllTagged(target, SkillType.Force, 3);
                })
                .RemoveAction((target) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.ForceStun3);
                });
        }
    }
}
