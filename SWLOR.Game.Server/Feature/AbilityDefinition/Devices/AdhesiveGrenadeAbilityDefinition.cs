using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class AdhesiveGrenadeAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            AdhesiveGrenade1();
            AdhesiveGrenade2();
            AdhesiveGrenade3();

            return _builder.Build();
        }

        private void Impact(uint activator, uint target, bool immobilizes, float duration)
        {
            if (GetFactionEqual(activator, target))
                return;

            var statusEffect = typeof(HobbleStatusEffect);
            if (immobilizes)
            {
                statusEffect = typeof(ImmobilizedStatusEffect);
            }

            StatusEffect.ApplyStatusEffect(activator, target, statusEffect, duration, CombatDamageType.Physical);

            CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
            Enmity.ModifyEnmity(activator, target, 150);
        }

        private void AdhesiveGrenade1()
        {
            _builder.Create(FeatType.AdhesiveGrenade1, PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade I")
                .Level(1)
                .HasRecastDelay(RecastGroup.AdhesiveGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Fnf_Dispel_Greater), string.Empty, RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, false, 4f);
                    });
                });
        }

        private void AdhesiveGrenade2()
        {
            _builder.Create(FeatType.AdhesiveGrenade2, PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade II")
                .Level(2)
                .HasRecastDelay(RecastGroup.AdhesiveGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Fnf_Dispel_Greater), string.Empty, RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, true, 6f);
                    });
                });
        }

        private void AdhesiveGrenade3()
        {
            _builder.Create(FeatType.AdhesiveGrenade3, PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade III")
                .Level(3)
                .HasRecastDelay(RecastGroup.AdhesiveGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Fnf_Dispel_Greater), string.Empty, RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, true, 8f);
                    });
                });
        }
    }
}
