using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Model;
using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Devices
{
    public class KoltoGrenadeAbilityDefinition : ExplosiveBaseAbilityDefinition
    {

        public KoltoGrenadeAbilityDefinition(IRandomService random, IItemService itemService, IPerkService perkService, IStatService statService, ICombatService combatService, ICombatPointService combatPointService, IEnmityService enmityService, IStatusEffectService statusEffectService) 
            : base(random, itemService, perkService, statService, combatService, combatPointService, enmityService, statusEffectService)
        {
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            KoltoGrenade1(builder);
            KoltoGrenade2(builder);
            KoltoGrenade3(builder);

            return builder.Build();
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

            _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
            _enmityService.ModifyEnmityOnAll(activator, 180);
        }

        private void KoltoGrenade1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.KoltoGrenade1, PerkType.KoltoGrenade)
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

        private void KoltoGrenade2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.KoltoGrenade2, PerkType.KoltoGrenade)
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

        private void KoltoGrenade3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.KoltoGrenade3, PerkType.KoltoGrenade)
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
