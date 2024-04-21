using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.NWScript.Enum.VisualEffect;
using SWLOR.Core.Service;
using SWLOR.Core.Service.AbilityService;
using SWLOR.Core.Service.PerkService;
using SWLOR.Core.Service.SkillService;
using SWLOR.Core.Service.StatusEffectService;

namespace SWLOR.Core.Feature.AbilityDefinition.FirstAid
{
    public class ShieldingAbilityDefinition: FirstAidBaseAbilityDefinition
    {
        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Shielding1();
            Shielding2();
            Shielding3();
            Shielding4();

            return Builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location location)
        {
            if (!IsWithinRange(activator, target))
            {
                return "Your target is too far away.";
            }

            if (!HasStimPack(activator))
            {
                return "You have no stim packs.";
            }

            return string.Empty;
        }

        private void Impact(uint activator, uint target, StatusEffectType statusEffectType)
        {
            var willpowerMod = GetAbilityModifier(AbilityType.Willpower, activator);
            const float BaseLength = 900f;
            var length = BaseLength + willpowerMod * 30f;
            
            StatusEffect.Apply(activator, target, statusEffectType, length);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Spell_Mantle_Use), target);

            TakeStimPack(activator);
        }

        private void Shielding1()
        {
            Builder.Create(FeatType.Shielding1, PerkType.Shielding)
                .Name("Shielding I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Shielding, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, StatusEffectType.Shielding1);

                    Enmity.ModifyEnmityOnAll(activator, 150);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }

        private void Shielding2()
        {
            Builder.Create(FeatType.Shielding2, PerkType.Shielding)
                .Name("Shielding II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Shielding, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, StatusEffectType.Shielding2);

                    Enmity.ModifyEnmityOnAll(activator, 300);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }

        private void Shielding3()
        {
            Builder.Create(FeatType.Shielding3, PerkType.Shielding)
                .Name("Shielding III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Shielding, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, StatusEffectType.Shielding3);

                    Enmity.ModifyEnmityOnAll(activator, 450);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }

        private void Shielding4()
        {
            Builder.Create(FeatType.Shielding4, PerkType.Shielding)
                .Name("Shielding IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.Shielding, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(6)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, StatusEffectType.Shielding4);

                    Enmity.ModifyEnmityOnAll(activator, 600);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }
    }
}
