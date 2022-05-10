using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item.Property;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class BurstOfSpeedStatusEffectDefinition : IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            BurstOfSpeed1(builder);
            BurstOfSpeed2(builder);

            return builder.Build();
        }

        private void BurstOfSpeed1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.BurstOfSpeed1)
                .Name("Burst of Speed I")
                .CannotReplace(StatusEffectType.BurstOfSpeed2)
                .GrantAction((source, target, length, effectData) =>
                {
                    var effect = EffectMovementSpeedIncrease(15);
                    effect = EffectLinkEffects(EffectACIncrease(1), effect);
                    effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.BurstOfSpeed1);
                    ApplyEffectToObject(DurationType.Permanent, effect, target);

                    CombatPoint.AddCombatPointToAllTagged(target, SkillType.Force, 3);
                })
                .RemoveAction((target, effectData) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.BurstOfSpeed1);
                });
        }
        private void BurstOfSpeed2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.BurstOfSpeed2)
                .Name("Burst of Speed II")
                .Replaces(StatusEffectType.BurstOfSpeed1)
                .GrantAction((source, target, length, effectData) =>
                {
                    var effect = EffectMovementSpeedIncrease(25);
                    effect = EffectLinkEffects(EffectACIncrease(2), effect);
                    effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.BurstOfSpeed2);
                    ApplyEffectToObject(DurationType.Permanent, effect, target);

                    CombatPoint.AddCombatPointToAllTagged(target, SkillType.Force, 3);
                })
                .RemoveAction((target, effectData) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.BurstOfSpeed2);
                });
        }
    }
}
