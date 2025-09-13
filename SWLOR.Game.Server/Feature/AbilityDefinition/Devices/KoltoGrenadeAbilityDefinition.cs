using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

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
        
        private void Impact(uint activator, uint target, int regenAmount)
        {
            if (!GetFactionEqual(activator, target))
                return;

            RemoveEffectByTag(target, "kolto_regen"); // Get rid of any regen effects to prevent stacking

            Effect eKolto = EffectRegenerate(regenAmount, 6f);
            eKolto = TagEffect(eKolto, "kolto_regen");

            ApplyEffectToObject(DurationType.Temporary, eKolto, target, 45f);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_G), target);

            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
            Enmity.ModifyEnmityOnAll(activator, 180);
        }

        private void KoltoGrenade1()
        {
            _builder.Create(FeatType.KoltoGrenade1, PerkType.KoltoGrenade)
                .Name("Kolto Grenade I")
                .Level(1)
                .HasRecastDelay(RecastGroup.KoltoGrenade, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
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
                .Level(2)
                .HasRecastDelay(RecastGroup.KoltoGrenade, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
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
                .Level(3)
                .HasRecastDelay(RecastGroup.KoltoGrenade, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(7)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
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
