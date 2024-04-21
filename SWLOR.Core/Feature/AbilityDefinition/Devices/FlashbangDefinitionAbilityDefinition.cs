using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.NWScript.Enum.VisualEffect;
using SWLOR.Core.Service;
using SWLOR.Core.Service.AbilityService;
using SWLOR.Core.Service.PerkService;
using SWLOR.Core.Service.SkillService;

namespace SWLOR.Core.Feature.AbilityDefinition.Devices
{
    public class FlashbangDefinitionAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            FlashbangGrenade1();
            FlashbangGrenade2();
            FlashbangGrenade3();

            return _builder.Build();
        }
        
        private void Impact(uint activator, uint target, int abReduce)
        {
            if (GetFactionEqual(activator, target))
                return;

            ApplyEffectToObject(DurationType.Temporary, EffectAccuracyDecrease(abReduce), target, 20f);

            CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
            Enmity.ModifyEnmity(activator, target, 250);
        }

        private void FlashbangGrenade1()
        {
            _builder.Create(FeatType.FlashbangGrenade1, PerkType.FlashbangGrenade)
                .Name("Flashbang Grenade I")
                .Level(1)
                .HasRecastDelay(RecastGroup.FlashbangGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(1)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Mystical_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 2);
                    });
                });
        }

        private void FlashbangGrenade2()
        {
            _builder.Create(FeatType.FlashbangGrenade2, PerkType.FlashbangGrenade)
                .Name("Flashbang Grenade II")
                .Level(2)
                .HasRecastDelay(RecastGroup.FlashbangGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(2)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Mystical_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 4);
                    });
                });
        }

        private void FlashbangGrenade3()
        {
            _builder.Create(FeatType.FlashbangGrenade3, PerkType.FlashbangGrenade)
                .Name("Flashbang Grenade III")
                .Level(3)
                .HasRecastDelay(RecastGroup.FlashbangGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Mystical_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 6);
                    });
                });
        }
    }
}
