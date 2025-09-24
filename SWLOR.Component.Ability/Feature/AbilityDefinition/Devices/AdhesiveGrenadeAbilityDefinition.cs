using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Combat.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Devices
{
    public class AdhesiveGrenadeAbilityDefinition : ExplosiveBaseAbilityDefinition
    {

        public AdhesiveGrenadeAbilityDefinition(IRandomService random, IItemService itemService, IPerkService perkService, IStatService statService, ICombatService combatService, ICombatPointService combatPointService, IEnmityService enmityService, IStatusEffectService statusEffectService) 
            : base(random, itemService, perkService, statService, combatService, combatPointService, enmityService, statusEffectService)
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
                var dc = _combatService.CalculateSavingThrowDC(activator, SavingThrow.Fortitude, immobilizeDC);
                var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
                if (checkResult == SavingThrowResultType.Failed)
                {
                    effect = EffectCutsceneImmobilize();
                }
            }
            
            ApplyEffectToObject(DurationType.Temporary, effect, target, slowLength);

            _combatPointService.AddCombatPoint(activator, target, SkillType.Devices, 3);
            _enmityService.ModifyEnmity(activator, target, 150);
        }

        private void AdhesiveGrenade1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.AdhesiveGrenade1, PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade I")
                .Level(1)
                .HasRecastDelay(RecastGroup.AdhesiveGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Fnf_Dispel_Greater), string.Empty, RadiusSize.Large, (target) =>
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
                .HasRecastDelay(RecastGroup.AdhesiveGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Fnf_Dispel_Greater), string.Empty, RadiusSize.Large, (target) =>
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
                .HasRecastDelay(RecastGroup.AdhesiveGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Fnf_Dispel_Greater), string.Empty, RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 50, 8f, 12);
                    });
                });
        }
    }
}
