using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.FirstAid
{
    public class ShieldingAbilityDefinition: FirstAidBaseAbilityDefinition
    {
        public ShieldingAbilityDefinition(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Shielding1(builder);
            Shielding2(builder);
            Shielding3(builder);
            Shielding4(builder);

            return builder.Build();
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
            
            StatusEffectService.Apply(activator, target, statusEffectType, length);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Spell_Mantle_Use), target);

            TakeStimPack(activator);
        }

        private void Shielding1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Shielding1, PerkType.Shielding)
                .Name("Shielding I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Shielding, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(3)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, StatusEffectType.Shielding1);

                    EnmityService.ModifyEnmityOnAll(activator, 150);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }

        private void Shielding2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Shielding2, PerkType.Shielding)
                .Name("Shielding II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Shielding, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(4)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, StatusEffectType.Shielding2);

                    EnmityService.ModifyEnmityOnAll(activator, 300);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }

        private void Shielding3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Shielding3, PerkType.Shielding)
                .Name("Shielding III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Shielding, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(5)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, StatusEffectType.Shielding3);

                    EnmityService.ModifyEnmityOnAll(activator, 450);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }

        private void Shielding4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Shielding4, PerkType.Shielding)
                .Name("Shielding IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.Shielding, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(6)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, StatusEffectType.Shielding4);

                    EnmityService.ModifyEnmityOnAll(activator, 600);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }
    }
}
