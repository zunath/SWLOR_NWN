using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class BurstOfSpeedStatusEffectDefinition : IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            BurstOfSpeed1(builder);
            BurstOfSpeed2(builder);
            BurstOfSpeed3(builder);
            BurstOfSpeed4(builder);
            BurstOfSpeed5(builder);

            return builder.Build();
        }

        private void BurstOfSpeed1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.BurstOfSpeed1)
                .Name("Burst of Speed I")
                .GrantAction((source, target, length, effectData) =>
                {
                    var effect = EffectMovementSpeedIncrease(20);
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
                .GrantAction((source, target, length, effectData) =>
                {
                    var effect = EffectMovementSpeedIncrease(30);
                    effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.BurstOfSpeed2);
                    ApplyEffectToObject(DurationType.Permanent, effect, target);

                    CombatPoint.AddCombatPointToAllTagged(target, SkillType.Force, 3);
                })
                .RemoveAction((target, effectData) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.BurstOfSpeed2);
                });
        }
        private void BurstOfSpeed3(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.BurstOfSpeed3)
                .Name("Burst of Speed III")
                .GrantAction((source, target, length, effectData) =>
                {
                    var effect = EffectMovementSpeedIncrease(40);
                    effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.BurstOfSpeed3);
                    ApplyEffectToObject(DurationType.Permanent, effect, target);

                    CombatPoint.AddCombatPointToAllTagged(target, SkillType.Force, 3);
                })
                .RemoveAction((target, effectData) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.BurstOfSpeed3);
                });
        }
        private void BurstOfSpeed4(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.BurstOfSpeed4)
                .Name("Burst of Speed IV")
                .GrantAction((source, target, length, effectData) =>
                {
                    var effect = EffectMovementSpeedIncrease(50);
                    effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.BurstOfSpeed4);
                    ApplyEffectToObject(DurationType.Permanent, effect, target);

                    CombatPoint.AddCombatPointToAllTagged(target, SkillType.Force, 3);
                })
                .RemoveAction((target, effectData) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.BurstOfSpeed4);
                });
        }
        private void BurstOfSpeed5(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.BurstOfSpeed5)
                .Name("Burst of Speed V")
                .GrantAction((source, target, length, effectData) =>
                {
                    var effect = EffectMovementSpeedIncrease(60);
                    effect = TagEffect(effect, "StatusEffectType." + StatusEffectType.BurstOfSpeed5);
                    ApplyEffectToObject(DurationType.Permanent, effect, target);

                    CombatPoint.AddCombatPointToAllTagged(target, SkillType.Force, 3);
                })
                .RemoveAction((target, effectData) =>
                {
                    RemoveEffectByTag(target, "StatusEffectType." + StatusEffectType.BurstOfSpeed5);
                });
        }
    }
}
