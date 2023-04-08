using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

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

        private void Impact(uint activator, uint target, int immobilizeChance, float slowLength, int immobilizeDC)
        {
            if (GetFactionEqual(activator, target))
                return;

            var effect = EffectSlow();
            if (immobilizeDC > 0)
            {
                var dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Fortitude, immobilizeDC);
                var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
                if (checkResult == SavingThrowResultType.Failed)
                {
                    effect = EffectCutsceneImmobilize();
                }
            }
            
            ApplyEffectToObject(DurationType.Temporary, effect, target, slowLength);

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
