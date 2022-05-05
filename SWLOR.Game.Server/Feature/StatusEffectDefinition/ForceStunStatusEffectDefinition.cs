using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class ForceStunStatusEffectDefinition: IStatusEffectListDefinition
    {
        private const float AOESize = RadiusSize.Medium;

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            ForceStun1(builder);
            ForceStun2(builder);
            ForceStun3(builder);

            return builder.Build();
        }

        private void Impact(uint source, uint target, StatusEffectType type)
        {
            if (!Ability.GetAbilityResisted(source, target, "Force Stun"))
            {
                var effect = EffectDazed();
                effect = EffectLinkEffects(effect, EffectVisualEffect(VisualEffect.Vfx_Dur_Iounstone_Blue));
                effect = TagEffect(effect, "StatusEffectType." + type);
                ApplyEffectToObject(DurationType.Temporary, effect, target, 6.1f);
            }
            else
            {
                var effect = EffectAttackDecrease(2);
                effect = EffectLinkEffects(effect, EffectACDecrease(2));
                effect = TagEffect(effect, "StatusEffectType." + type);
                ApplyEffectToObject(DurationType.Temporary, effect, target, 6.1f);
            }
            
            CombatPoint.AddCombatPoint(source, target, SkillType.Force, 3);

            Enmity.ModifyEnmity(source, target, 10);
        }

        private void ForceStun1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceStun1)
                .Name("Force Stun I")
                .EffectIcon(EffectIconType.Dazed)
                .GrantAction((source, target, length, data) =>
                {
                    Impact(source, target, StatusEffectType.ForceStun1);
                })
                .TickAction((source, target, data) => 
                {
                    Impact(source, target, StatusEffectType.ForceStun1);
                })
                .RemoveAction((target, effectData) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.ForceStun1);
                });
        }
        private void ForceStun2(StatusEffectBuilder builder)
        {
            void ForceStun2Impact(uint source, uint target)
            {
                Impact(source, target, StatusEffectType.ForceStun2);

                // Target the next nearest creature and do the same thing.
                var targetCreature = GetFirstObjectInShape(Shape.Sphere, AOESize, GetLocation(target), true);
                while (GetIsObjectValid(targetCreature))
                {
                    if (targetCreature != target && GetIsReactionTypeHostile(targetCreature, source))
                    {
                        // Apply to nearest other creature, then exit loop. 
                        // Intentionally applying Force Stun I so that it doesn't continue to chain exponentially.
                        StatusEffect.Apply(source, targetCreature, StatusEffectType.ForceStun1, 6.1f);
                        break;
                    }
                    targetCreature = GetNextObjectInShape(Shape.Sphere, AOESize, GetLocation(target), true);
                }
            }

            builder.Create(StatusEffectType.ForceStun2)
                .Name("Force Stun II")
                .EffectIcon(EffectIconType.Dazed) 
                .GrantAction((source, target, length, data) =>
                {
                    ForceStun2Impact(source, target);
                })
                .TickAction((source, target, data) =>
                {
                    ForceStun2Impact(source, target);
                })
                .RemoveAction((target, effectData) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.ForceStun2);
                });
        }
        private void ForceStun3(StatusEffectBuilder builder)
        {
            void ForceStun3Impact(uint source, uint target)
            {
                Impact(source, target, StatusEffectType.ForceStun3);

                // Target the next nearest creature and do the same thing.
                var targetCreature = GetFirstObjectInShape(Shape.Sphere, AOESize, GetLocation(target), true);
                while (GetIsObjectValid(targetCreature))
                {
                    if (targetCreature != target && GetIsReactionTypeHostile(targetCreature, source))
                    {
                        // Apply to nearest other creature, then move on to the next.
                        // Intentionally applying Force Stun I so that it doesn't continue to chain exponentially.
                        StatusEffect.Apply(source, targetCreature, StatusEffectType.ForceStun1, 6.1f);
                    }
                    targetCreature = GetNextObjectInShape(Shape.Sphere, AOESize, GetLocation(target), true);
                }
            }

            builder.Create(StatusEffectType.ForceStun3)
                .Name("Force Stun III")
                .EffectIcon(EffectIconType.Dazed) 
                .GrantAction((source, target, length, data) =>
                {
                    ForceStun3Impact(source, target);
                })
                .TickAction((source, target, data) =>
                {
                    ForceStun3Impact(source, target);
                })
                .RemoveAction((target, effectData) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.ForceStun3);
                });
        }
    }
}
