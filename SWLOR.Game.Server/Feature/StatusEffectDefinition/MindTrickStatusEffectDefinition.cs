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
    public class MindTrickStatusEffectDefinition: IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            MindTrick1(builder);
            MindTrick2(builder);

            return builder.Build();
        }

        private void MindTrick1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.MindTrick1)
                .Name("Mind Trick I")
                .EffectIcon(13) // 13 = Confused
                .GrantAction((source, target, length) =>
                {
                    if (!Ability.GetAbilityResisted(source, target, AbilityType.Intelligence, AbilityType.Wisdom))
                    {
                        var effect = EffectConfused();
                        effect = EffectLinkEffects(effect, EffectVisualEffect(VisualEffect.Vfx_Imp_Confusion_S));
                        effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.MindTrick1);
                        ApplyEffectToObject(DurationType.Permanent, effect, target);
                    }
                    CombatPoint.AddCombatPointToAllTagged(target, SkillType.Force, 3);
                })
                .RemoveAction((target) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.MindTrick1);
                });
        }

        private void MindTrick2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.MindTrick2)
                .Name("Mind Trick II")
                .EffectIcon(13) // 13 = Confused
                .GrantAction((source, target, length) =>
                {
                    const float radiusSize = RadiusSize.Medium;
                    if (!Ability.GetAbilityResisted(source, target, AbilityType.Intelligence, AbilityType.Wisdom))
                    {
                        var effect = EffectConfused();
                        effect = EffectLinkEffects(effect, EffectVisualEffect(VisualEffect.Vfx_Imp_Confusion_S));
                        effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.MindTrick2);
                        ApplyEffectToObject(DurationType.Permanent, effect, target);
                    }

                    // Target the next nearest creature and do the same thing.
                    var targetCreature = GetFirstObjectInShape(Shape.Sphere, radiusSize, GetLocation(target), true);
                    while (GetIsObjectValid(targetCreature))
                    {
                        if (targetCreature != target && GetIsReactionTypeHostile(target, source) && 
                            !(GetRacialType(targetCreature) == RacialType.Cyborg || GetRacialType(targetCreature) == RacialType.Robot))
                        {
                            // Apply to nearest other creature, then move on to the next.
                            // Intentionally applying Mind Trick I so that it doesn't continue to chain exponentially.
                            StatusEffect.Apply(source, target, StatusEffectType.MindTrick1, 0f);
                        }
                        targetCreature = GetNextObjectInShape(Shape.Sphere, radiusSize, GetLocation(target), true);
                    }
                    CombatPoint.AddCombatPointToAllTagged(target, SkillType.Force, 3);
                })
                .RemoveAction((target) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.MindTrick2);
                });
        }
    }
}
