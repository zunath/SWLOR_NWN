using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public class MedKitAbilityDefinition: FirstAidBaseAbilityDefinition
    {
        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            MedKit1();
            MedKit2();
            MedKit3();
            MedKit4();
            MedKit5();

            return Builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location location)
        {
            if (!IsWithinRange(activator, target))
            {
                return "Your target is too far away.";
            }

            if (GetCurrentHitPoints(target) >= GetMaxHitPoints(target))
            {
                return "Your target is unharmed.";
            }

            if (!HasMedicalSupplies(activator))
            {
                return "You have no medical supplies.";
            }

            return string.Empty;
        }

        private void Impact(uint activator, uint target, int baseAmount)
        {
            var willpowerMod = GetAbilityModifier(AbilityType.Willpower, activator);
            var amount = baseAmount + willpowerMod * 10 + Random.D10(1);

            // limit xp to amount *actually* healed.
            if (amount > (GetMaxHitPoints(target) - GetCurrentHitPoints(target)))
            {
                amount = GetMaxHitPoints(target) - GetCurrentHitPoints(target);
            }

            Service.Skill.GiveSkillXP(activator, SkillType.FirstAid, 3 * amount);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Heal), target);
            TakeMedicalSupplies(activator);
        }

        private void MedKit1()
        {
            Builder.Create(FeatType.MedKit1, PerkType.MedKit)
                .Name("Med Kit I")
                .HasRecastDelay(RecastGroup.MedKit, 6f)
                .HasActivationDelay(2f)
                .RequirementStamina(1)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 30);
                });
        }

        private void MedKit2()
        {
            Builder.Create(FeatType.MedKit2, PerkType.MedKit)
                .Name("Med Kit II")
                .HasRecastDelay(RecastGroup.MedKit, 6f)
                .HasActivationDelay(2f)
                .RequirementStamina(2)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 50);
                });
        }

        private void MedKit3()
        {
            Builder.Create(FeatType.MedKit3, PerkType.MedKit)
                .Name("Med Kit III")
                .HasRecastDelay(RecastGroup.MedKit, 6f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 90);
                });
        }

        private void MedKit4()
        {
            Builder.Create(FeatType.MedKit4, PerkType.MedKit)
                .Name("Med Kit IV")
                .HasRecastDelay(RecastGroup.MedKit, 6f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 140);
                });
        }
        private void MedKit5()
        {
            Builder.Create(FeatType.MedKit5, PerkType.MedKit)
                .Name("Med Kit V")
                .HasRecastDelay(RecastGroup.MedKit, 6f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 180);
                });
        }
    }
}
