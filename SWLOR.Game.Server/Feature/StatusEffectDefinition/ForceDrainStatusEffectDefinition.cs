using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Service.Random;

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
                .EffectIcon(EffectIconType.LevelDrain)
                .CannotReplace(StatusEffectType.ForceDrain2, StatusEffectType.ForceDrain3, StatusEffectType.ForceDrain4, StatusEffectType.ForceDrain5)
                .GrantAction((source, target, length, effectData) =>
                {
                    var willBonus = GetAbilityScore(source, AbilityType.Willpower);
                    var willDMG = willBonus + Random.D2(willBonus / 3);
                    ProcessForceDrainTick(VisualEffect.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    Enmity.ModifyEnmityOnAll(source, 200);

                    CombatPoint.AddCombatPoint(source, target, SkillType.Force, 3);
                })
                .TickAction((source, target, effectData) =>
                {
                    var willBonus = GetAbilityScore(source, AbilityType.Willpower);
                    var willDMG = willBonus + Random.D2(willBonus / 3);
                    ProcessForceDrainTick(VisualEffect.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    Enmity.ModifyEnmityOnAll(source, 75);

                    CombatPoint.AddCombatPoint(source, target, SkillType.Force, 3);
                });
        }
        private void ForceDrain2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceDrain2)
                .Name("Force Drain II")
                .EffectIcon(EffectIconType.LevelDrain)
                .Replaces(StatusEffectType.ForceDrain1)
                .CannotReplace(StatusEffectType.ForceDrain3, StatusEffectType.ForceDrain4, StatusEffectType.ForceDrain5)
                .GrantAction((source, target, length, effectData) =>
                {
                    var willBonus = GetAbilityScore(source, AbilityType.Willpower);
                    var willDMG = 10 + willBonus + Random.D3(willBonus / 3);
                    ProcessForceDrainTick(VisualEffect.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    Enmity.ModifyEnmityOnAll(source, 250);

                    CombatPoint.AddCombatPoint(source, target, SkillType.Force, 3);
                })
                .TickAction((source, target, effectData) =>
                {
                    var willBonus = GetAbilityScore(source, AbilityType.Willpower);
                    var willDMG = 10 + willBonus + Random.D3(willBonus / 3);
                    ProcessForceDrainTick(VisualEffect.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    Enmity.ModifyEnmityOnAll(source, 100);

                    CombatPoint.AddCombatPoint(source, target, SkillType.Force, 3);
                });
        }
        private void ForceDrain3(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceDrain3)
                .Name("Force Drain III")
                .EffectIcon(EffectIconType.LevelDrain)
                .Replaces(StatusEffectType.ForceDrain1, StatusEffectType.ForceDrain2)
                .CannotReplace(StatusEffectType.ForceDrain4, StatusEffectType.ForceDrain5)
                .GrantAction((source, target, length, effectData) =>
                {
                    var willBonus = GetAbilityScore(source, AbilityType.Willpower);
                    var willDMG = 15 + willBonus + Random.D4(willBonus / 3);
                    ProcessForceDrainTick(VisualEffect.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    Enmity.ModifyEnmityOnAll(source, 250);

                    CombatPoint.AddCombatPoint(source, target, SkillType.Force, 3);
                })
                .TickAction((source, target, effectData) =>
                {
                    var willBonus = GetAbilityScore(source, AbilityType.Willpower);
                    var willDMG = 15 + willBonus + Random.D4(willBonus / 3);
                    ProcessForceDrainTick(VisualEffect.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    Enmity.ModifyEnmityOnAll(source, 125);

                    CombatPoint.AddCombatPoint(source, target, SkillType.Force, 3);
                });
        }
        private void ForceDrain4(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceDrain4)
                .Name("Force Drain IV")
                .EffectIcon(EffectIconType.LevelDrain)
                .Replaces(StatusEffectType.ForceDrain1, StatusEffectType.ForceDrain2, StatusEffectType.ForceDrain3)
                .CannotReplace(StatusEffectType.ForceDrain5)
                .GrantAction((source, target, length, effectData) =>
                {
                    var willBonus = GetAbilityScore(source, AbilityType.Willpower);
                    var willDMG = 20 + willBonus + Random.D6(willBonus / 3);
                    ProcessForceDrainTick(VisualEffect.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    Enmity.ModifyEnmityOnAll(source, 300);

                    CombatPoint.AddCombatPoint(source, target, SkillType.Force, 3);
                })
                .TickAction((source, target, effectData) =>
                {
                    var willBonus = GetAbilityScore(source, AbilityType.Willpower);
                    var willDMG = 20 + willBonus + Random.D6(willBonus / 3);
                    ProcessForceDrainTick(VisualEffect.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    Enmity.ModifyEnmityOnAll(source, 150);

                    CombatPoint.AddCombatPoint(source, target, SkillType.Force, 3);
                });
        }
        private void ForceDrain5(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceDrain5)
                .Name("Force Drain V")
                .EffectIcon(EffectIconType.LevelDrain)
                .Replaces(StatusEffectType.ForceDrain1, StatusEffectType.ForceDrain2, StatusEffectType.ForceDrain3, StatusEffectType.ForceDrain4)
                .GrantAction((source, target, length, effectData) =>
                {
                    var willBonus = GetAbilityScore(source, AbilityType.Willpower);
                    var willDMG = 25 + willBonus + Random.D8(willBonus / 3);
                    ProcessForceDrainTick(VisualEffect.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    Enmity.ModifyEnmityOnAll(source, 350);

                    CombatPoint.AddCombatPoint(source, target, SkillType.Force, 3);
                })
                .TickAction((source, target, effectData) =>
                {
                    var willBonus = GetAbilityScore(source, AbilityType.Willpower);
                    var willDMG = 25 + willBonus + Random.D8(willBonus / 3);
                    ProcessForceDrainTick(VisualEffect.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    Enmity.ModifyEnmityOnAll(source, 175);

                    CombatPoint.AddCombatPoint(source, target, SkillType.Force, 3);
                });
        }

        private void ProcessForceDrainTick(VisualEffect vfx, int damage, int heal, uint target, uint source)
        {

            // Check if caster and target are within 20.0f range limit
            var distance = GetDistanceBetween(source, target);
            if (distance > 20.0f)
            {
                // End the concentration ability if range is exceeded
                Ability.EndConcentrationAbility(source);
                SendMessageToPC(source, "Your Force Drain connection has been broken due to exceeding a distance of 20m.");
                return;
            }


            var dc = Combat.CalculateSavingThrowDC(source, SavingThrow.Will, 14);
            var checkResult = WillSave(target, dc, SavingThrowType.None, source);

            if (checkResult == SavingThrowResultType.Failed)
            {
                PlaySound("plr_force_absorb");

                AssignCommand(source, () =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectBeam(vfx, target, BodyNode.Hand), source, 2.0f);
                    ApplyEffectToObject(DurationType.Temporary, EffectBeam(vfx, source, BodyNode.Hand), target, 2.0f);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Negative_Energy), target);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Reduce_Ability_Score), target);
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(heal), source);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Pulse_Negative), source);
                });
            }
        }
    }
}