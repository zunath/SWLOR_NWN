using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Combat.ValueObjects;

namespace SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition
{
    public class ForceDrainStatusEffectDefinition : IStatusEffectListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public ForceDrainStatusEffectDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IRandomService Random => _serviceProvider.GetRequiredService<IRandomService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
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
                    ProcessForceDrainTick(VisualEffectType.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    EnmityService.ModifyEnmityOnAll(source, 200);

                    CombatPointService.AddCombatPoint(source, target, SkillType.Force, 3);
                })
                .TickAction((source, target, effectData) =>
                {
                    var willBonus = GetAbilityScore(source, AbilityType.Willpower);
                    var willDMG = willBonus + Random.D2(willBonus / 3);
                    ProcessForceDrainTick(VisualEffectType.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    EnmityService.ModifyEnmityOnAll(source, 75);

                    CombatPointService.AddCombatPoint(source, target, SkillType.Force, 3);
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
                    ProcessForceDrainTick(VisualEffectType.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    EnmityService.ModifyEnmityOnAll(source, 250);

                    CombatPointService.AddCombatPoint(source, target, SkillType.Force, 3);
                })
                .TickAction((source, target, effectData) =>
                {
                    var willBonus = GetAbilityScore(source, AbilityType.Willpower);
                    var willDMG = 10 + willBonus + Random.D3(willBonus / 3);
                    ProcessForceDrainTick(VisualEffectType.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    EnmityService.ModifyEnmityOnAll(source, 100);

                    CombatPointService.AddCombatPoint(source, target, SkillType.Force, 3);
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
                    ProcessForceDrainTick(VisualEffectType.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    EnmityService.ModifyEnmityOnAll(source, 250);

                    CombatPointService.AddCombatPoint(source, target, SkillType.Force, 3);
                })
                .TickAction((source, target, effectData) =>
                {
                    var willBonus = GetAbilityScore(source, AbilityType.Willpower);
                    var willDMG = 15 + willBonus + Random.D4(willBonus / 3);
                    ProcessForceDrainTick(VisualEffectType.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    EnmityService.ModifyEnmityOnAll(source, 125);

                    CombatPointService.AddCombatPoint(source, target, SkillType.Force, 3);
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
                    ProcessForceDrainTick(VisualEffectType.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    EnmityService.ModifyEnmityOnAll(source, 300);

                    CombatPointService.AddCombatPoint(source, target, SkillType.Force, 3);
                })
                .TickAction((source, target, effectData) =>
                {
                    var willBonus = GetAbilityScore(source, AbilityType.Willpower);
                    var willDMG = 20 + willBonus + Random.D6(willBonus / 3);
                    ProcessForceDrainTick(VisualEffectType.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    EnmityService.ModifyEnmityOnAll(source, 150);

                    CombatPointService.AddCombatPoint(source, target, SkillType.Force, 3);
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
                    ProcessForceDrainTick(VisualEffectType.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    EnmityService.ModifyEnmityOnAll(source, 350);

                    CombatPointService.AddCombatPoint(source, target, SkillType.Force, 3);
                })
                .TickAction((source, target, effectData) =>
                {
                    var willBonus = GetAbilityScore(source, AbilityType.Willpower);
                    var willDMG = 25 + willBonus + Random.D8(willBonus / 3);
                    ProcessForceDrainTick(VisualEffectType.Vfx_Beam_Drain, willDMG, willDMG, target, source);
                    EnmityService.ModifyEnmityOnAll(source, 175);

                    CombatPointService.AddCombatPoint(source, target, SkillType.Force, 3);
                });
        }

        private void ProcessForceDrainTick(VisualEffectType vfx, int damage, int heal, uint target, uint source)
        {
            var dc = CombatService.CalculateSavingThrowDC(source, SavingThrowCategoryType.Will, 14);
            var checkResult = WillSave(target, dc, SavingThrowType.None, source);

            if (checkResult == SavingThrowResultType.Failed)
            {
                PlaySound("plr_force_absorb");

                AssignCommand(source, () =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectBeam(vfx, target, BodyNodeType.Hand), source, 2.0f);
                    ApplyEffectToObject(DurationType.Temporary, EffectBeam(vfx, source, BodyNodeType.Hand), target, 2.0f);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Negative_Energy), target);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Reduce_Ability_Score), target);
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(heal), source);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Pulse_Negative), source);
                });
            }
        }
    }
}
