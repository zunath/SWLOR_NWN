using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class KoltoGrenadeAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            KoltoGrenade1();
            KoltoGrenade2();
            KoltoGrenade3();

            return _builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location location)
        {
            if (!HasExplosives(activator))
            {
                return "You have no explosives.";
            }

            return string.Empty;
        }

        private void Impact(uint activator, uint target, int regenAmount)
        {
            if (!GetFactionEqual(activator, target))
                return;

            ApplyEffectToObject(DurationType.Temporary, EffectRegenerate(regenAmount, 6f), target, 45f);

            CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
            Enmity.ModifyEnmity(activator, target, 20);
        }

        private void KoltoGrenade1()
        {
            _builder.Create(FeatType.KoltoGrenade1, PerkType.KoltoGrenade)
                .Name("Kolto Grenade I")
                .HasRecastDelay(RecastGroup.Grenades, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(1)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Gas_Explosion_Nature), string.Empty, RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 6);
                    });
                });
        }

        private void KoltoGrenade2()
        {
            _builder.Create(FeatType.KoltoGrenade2, PerkType.KoltoGrenade)
                .Name("Kolto Grenade II")
                .HasRecastDelay(RecastGroup.Grenades, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Gas_Explosion_Nature), string.Empty, RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 14);
                    });
                });
        }

        private void KoltoGrenade3()
        {
            _builder.Create(FeatType.KoltoGrenade3, PerkType.KoltoGrenade)
                .Name("Kolto Grenade III")
                .HasRecastDelay(RecastGroup.Grenades, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Gas_Explosion_Nature), string.Empty, RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 24);
                    });
                });
        }
    }
}
