using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public class ResuscitationAbilityDefinition: FirstAidBaseAbilityDefinition
    {
        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Resuscitation1();
            Resuscitation2();
            Resuscitation3();

            return Builder.Build();
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
            Ability.ReapplyPlayerAuraAOE(target);

            if (hp > 0)
            {
                ApplyEffectToObject(DurationType.Instant, EffectHeal(hp), target);
            }

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Heal), target);
            TakeMedicalSupplies(activator);
        }

        private void Resuscitation1()
        {
            Builder.Create(FeatType.Resuscitation1, PerkType.Resuscitation)
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

                    Enmity.ModifyEnmityOnAll(activator, 800);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }

        private void Resuscitation2()
        {
            Builder.Create(FeatType.Resuscitation2, PerkType.Resuscitation)
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

                    Enmity.ModifyEnmityOnAll(activator, 1400);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }

        private void Resuscitation3()
        {
            Builder.Create(FeatType.Resuscitation3, PerkType.Resuscitation)
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

                    Enmity.ModifyEnmityOnAll(activator, 2500);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }
    }
}
