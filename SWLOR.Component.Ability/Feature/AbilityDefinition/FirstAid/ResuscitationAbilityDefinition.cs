using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Model;
using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.FirstAid
{
    public class ResuscitationAbilityDefinition: FirstAidBaseAbilityDefinition
    {
        public ResuscitationAbilityDefinition(
            IRandomService random, 
            IPerkService perkService, 
            ICombatPointService combatPointService, 
            IEnmityService enmityService, 
            IAbilityService abilityService,
            IStatusEffectService statusEffect) 
            : base(
                random, 
                perkService, 
                combatPointService, 
                enmityService, 
                abilityService,
                statusEffect)
        {
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Resuscitation1(builder);
            Resuscitation2(builder);
            Resuscitation3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location location)
        {
            if (!IsWithinRange(activator, target))
            {
                return "Your target is too far away.";
            }

            if (GetCurrentHitPoints(target) > 0)
            {
                return "Your target is not unconscious.";
            }

            if (!HasMedicalSupplies(activator))
            {
                return "You have no medical supplies.";
            }

            return string.Empty;
        }

        private void Impact(uint activator, uint target, int tier)
        {
            var willpower = GetAbilityScore(activator, AbilityType.Willpower);
            var targetMaxHP = GetMaxHitPoints(target);
            int hp;

            switch (tier)
            {
                default:
                    hp = 0;
                    break;
                case 2:
                    hp = (int)(willpower * 0.01f * targetMaxHP);
                    break;
                case 3:
                    hp = (int)(2 * willpower * 0.01f * targetMaxHP);
                    break;
            }

            ApplyEffectToObject(DurationType.Instant, EffectResurrection(), target);
            AbilityService.ReapplyPlayerAuraAOE(target);

            if (hp > 0)
            {
                ApplyEffectToObject(DurationType.Instant, EffectHeal(hp), target);
            }

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Heal), target);
            TakeMedicalSupplies(activator);
        }

        private void Resuscitation1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Resuscitation1, PerkType.Resuscitation)
                .Name("Resuscitation I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Resuscitation, 180f)
                .HasActivationDelay(6f)
                .HasMaxRange(30.0f)
                .RequirementStamina(10)
                .UsesAnimation(Animation.LoopingGetLow)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 0);

                    EnmityService.ModifyEnmityOnAll(activator, 800);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }

        private void Resuscitation2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Resuscitation2, PerkType.Resuscitation)
                .Name("Resuscitation II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Resuscitation, 180f)
                .HasActivationDelay(6f)
                .HasMaxRange(30.0f)
                .RequirementStamina(10)
                .UsesAnimation(Animation.LoopingGetLow)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 25);

                    EnmityService.ModifyEnmityOnAll(activator, 1400);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }

        private void Resuscitation3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Resuscitation3, PerkType.Resuscitation)
                .Name("Resuscitation III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Resuscitation, 180f)
                .HasActivationDelay(6f)
                .HasMaxRange(30.0f)
                .RequirementStamina(10)
                .UsesAnimation(Animation.LoopingGetLow)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 50);

                    EnmityService.ModifyEnmityOnAll(activator, 2500);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }
    }
}
