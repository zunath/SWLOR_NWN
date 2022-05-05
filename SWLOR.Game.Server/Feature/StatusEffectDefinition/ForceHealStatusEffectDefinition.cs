using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class ForceHealStatusEffectDefinition : IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            ForceHeal1(builder);
            ForceHeal2(builder);
            ForceHeal3(builder);
            ForceHeal4(builder);
            ForceHeal5(builder);

            return builder.Build();
        }

        private void ForceHeal1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceHeal1)
                .Name("Force Heal I")
                .EffectIcon(EffectIconType.Regenerate)
                .TickAction((source, target, effectData) =>
                {
                    var amount = 10; // GetAbilityModifier(AbilityType.Wisdom, activator);

                    ApplyEffectToObject(DurationType.Instant, GetRacialType(target) == RacialType.Undead
                        ? EffectDamage(amount)
                        : EffectHeal(amount), target);

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_S), target);

                    Enmity.ModifyEnmityOnAll(source, amount);
                    CombatPoint.AddCombatPointToAllTagged(source, SkillType.Force, 3);
                });
        }
        private void ForceHeal2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceHeal2)
                .Name("Force Heal I")
                .EffectIcon(EffectIconType.Regenerate)
                .TickAction((source, target, effectData) =>
                {
                    var amount = 15; // GetAbilityModifier(AbilityType.Wisdom, activator);

                    ApplyEffectToObject(DurationType.Instant, GetRacialType(target) == RacialType.Undead
                        ? EffectDamage(amount)
                        : EffectHeal(amount), target);

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_S), target);

                    Enmity.ModifyEnmityOnAll(source, amount);
                    CombatPoint.AddCombatPointToAllTagged(source, SkillType.Force, 3);
                });
        }
        private void ForceHeal3(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceHeal3)
                .Name("Force Heal I")
                .EffectIcon(EffectIconType.Regenerate)
                .TickAction((source, target, effectData) =>
                {
                    var amount = 20; // GetAbilityModifier(AbilityType.Wisdom, activator);

                    ApplyEffectToObject(DurationType.Instant, GetRacialType(target) == RacialType.Undead
                        ? EffectDamage(amount)
                        : EffectHeal(amount), target);

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_S), target);

                    Enmity.ModifyEnmityOnAll(source, amount);
                    CombatPoint.AddCombatPointToAllTagged(source, SkillType.Force, 3);
                });
        }
        private void ForceHeal4(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceHeal4)
                .Name("Force Heal I")
                .EffectIcon(EffectIconType.Regenerate)
                .TickAction((source, target, effectData) =>
                {
                    var amount = 25; // GetAbilityModifier(AbilityType.Wisdom, activator);

                    ApplyEffectToObject(DurationType.Instant, GetRacialType(target) == RacialType.Undead
                        ? EffectDamage(amount)
                        : EffectHeal(amount), target);

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_S), target);

                    Enmity.ModifyEnmityOnAll(source, amount);
                    CombatPoint.AddCombatPointToAllTagged(source, SkillType.Force, 3);
                });
        }
        private void ForceHeal5(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceHeal5)
                .Name("Force Heal I")
                .EffectIcon(EffectIconType.Regenerate)
                .TickAction((source, target, effectData) =>
                {
                    var amount = 30; // GetAbilityModifier(AbilityType.Wisdom, activator);

                    ApplyEffectToObject(DurationType.Instant, GetRacialType(target) == RacialType.Undead
                        ? EffectDamage(amount)
                        : EffectHeal(amount), target);

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_S), target);

                    Enmity.ModifyEnmityOnAll(source, amount);
                    CombatPoint.AddCombatPointToAllTagged(source, SkillType.Force, 3);
                });
        }
    }
}
