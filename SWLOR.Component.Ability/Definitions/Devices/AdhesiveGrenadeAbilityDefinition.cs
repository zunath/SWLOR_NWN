using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Constants;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.Devices
{
    public class AdhesiveGrenadeAbilityDefinition : ExplosiveBaseAbilityDefinition
    {

        public AdhesiveGrenadeAbilityDefinition(IServiceProvider serviceProvider) 
            : base(serviceProvider)
        {
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            AdhesiveGrenade1(builder);
            AdhesiveGrenade2(builder);
            AdhesiveGrenade3(builder);

            return builder.Build();
        }

        private void Impact(uint activator, uint target, int immobilizeChance, float slowLength, int immobilizeDC)
        {
            if (GetFactionEqual(activator, target))
                return;

            var effect = EffectSlow();
            if (immobilizeDC > 0)
            {
                var dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Fortitude, immobilizeDC);
                var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
                if (checkResult == SavingThrowResultType.Failed)
                {
                    effect = EffectCutsceneImmobilize();
                }
            }
            
            ApplyEffectToObject(DurationType.Temporary, effect, target, slowLength);

            CombatPointService.AddCombatPoint(activator, target, SkillType.Devices, 3);
            EnmityService.ModifyEnmity(activator, target, 150);
        }

        private void AdhesiveGrenade1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.AdhesiveGrenade1, PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.AdhesiveGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffectType.Fnf_Dispel_Greater), string.Empty, RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 0, 4f, -1);
                    });
                });
        }

        private void AdhesiveGrenade2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.AdhesiveGrenade2, PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.AdhesiveGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffectType.Fnf_Dispel_Greater), string.Empty, RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 30, 6f, 8);
                    });
                });
        }

        private void AdhesiveGrenade3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.AdhesiveGrenade3, PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.AdhesiveGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffectType.Fnf_Dispel_Greater), string.Empty, RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 50, 8f, 12);
                    });
                });
        }
    }
}
