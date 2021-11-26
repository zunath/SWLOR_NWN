using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class ForceDrainStatusEffectDefinition : IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            ForceDrain1(builder);
            ForceDrain2(builder);
            ForceDrain3(builder);
            ForceDrain4(builder);
            ForceDrain5(builder);

            return builder.Build();
        }
        private void ForceDrain1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceDrain1)
                .Name("Force Drain I")
                .EffectIcon(25) // 25 = Haste
                .TickAction((source, target) =>
                {
                    if (!Ability.GetAbilityResisted(source, target))
                    {
                        DoDamageAndVFX(VisualEffect.Vfx_Beam_Drain, 1, 1, target, source);
                    }                   

                    Enmity.ModifyEnmityOnAll(source, 1);
                    CombatPoint.AddCombatPointToAllTagged(source, SkillType.Force, 3);
                });
        }
        private void ForceDrain2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceDrain2)
                .Name("Force Drain I")
                .EffectIcon(25) // 25 = Haste
                .TickAction((source, target) =>
                {
                    if (!Ability.GetAbilityResisted(source, target))
                    {
                        DoDamageAndVFX(VisualEffect.Vfx_Beam_Drain, 2, 2, target, source);
                    }

                    Enmity.ModifyEnmityOnAll(source, 1);
                    CombatPoint.AddCombatPointToAllTagged(source, SkillType.Force, 3);
                });
        }
        private void ForceDrain3(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceDrain3)
                .Name("Force Drain I")
                .EffectIcon(25) // 25 = Haste
                .TickAction((source, target) =>
                {
                    if (!Ability.GetAbilityResisted(source, target))
                    {
                        DoDamageAndVFX(VisualEffect.Vfx_Beam_Drain, 3, 3, target, source);
                    }

                    Enmity.ModifyEnmityOnAll(source, 1);
                    CombatPoint.AddCombatPointToAllTagged(source, SkillType.Force, 3);
                });
        }
        private void ForceDrain4(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceDrain4)
                .Name("Force Drain I")
                .EffectIcon(25) // 25 = Haste
                .TickAction((source, target) =>
                {
                    if (!Ability.GetAbilityResisted(source, target))
                    {
                        DoDamageAndVFX(VisualEffect.Vfx_Beam_Drain, 4, 4, target, source);
                    }

                    Enmity.ModifyEnmityOnAll(source, 1);
                    CombatPoint.AddCombatPointToAllTagged(source, SkillType.Force, 3);
                });
        }
        private void ForceDrain5(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceDrain5)
                .Name("Force Drain I")
                .EffectIcon(25) // 25 = Haste
                .TickAction((source, target) =>
                {
                    if (!Ability.GetAbilityResisted(source, target))
                    {
                        DoDamageAndVFX(VisualEffect.Vfx_Beam_Drain, 5, 5, target, source);
                    }

                    Enmity.ModifyEnmityOnAll(source, 1);
                    CombatPoint.AddCombatPointToAllTagged(source, SkillType.Force, 3);
                });
        }
        private void DoDamageAndVFX(VisualEffect vfx1, int damageAmt, int healAmt, uint target, uint source)
        {
            PlaySound("plr_force_absorb");
            ApplyEffectToObject(DurationType.Temporary, EffectBeam(vfx1, target, BodyNode.Hand), source, 2.0F);
            ApplyEffectToObject(DurationType.Temporary, EffectBeam(vfx1, source, BodyNode.Hand), target, 2.0F);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Negative_Energy), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Reduce_Ability_Score), target);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damageAmt), target);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(healAmt), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Pulse_Negative), source);
        }
    }
}
