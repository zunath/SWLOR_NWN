using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

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
                .EffectIcon(25) // 25 = Haste
                .TickAction((source, target) =>
                {
                    var amount = 2; // GetAbilityModifier(AbilityType.Wisdom, activator);

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
                .EffectIcon(25) // 25 = Haste
                .TickAction((source, target) =>
                {
                    var amount = 4; // GetAbilityModifier(AbilityType.Wisdom, activator);

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
                .EffectIcon(25) // 25 = Haste
                .TickAction((source, target) =>
                {
                    var amount = 6; // GetAbilityModifier(AbilityType.Wisdom, activator);

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
                .EffectIcon(25) // 25 = Haste
                .TickAction((source, target) =>
                {
                    var amount = 8; // GetAbilityModifier(AbilityType.Wisdom, activator);

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
                .EffectIcon(25) // 25 = Haste
                .TickAction((source, target) =>
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
    }
}
