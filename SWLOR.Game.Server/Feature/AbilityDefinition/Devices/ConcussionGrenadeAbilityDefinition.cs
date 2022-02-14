using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class ConcussionGrenadeAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            ConcussionGrenade1();
            ConcussionGrenade2();
            ConcussionGrenade3();

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

        private void Impact(uint activator, uint target, float dmg, int knockdownChance, float knockdownLength)
        {
            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Devices);

            var perception = GetAbilityModifier(AbilityType.Perception, activator);
            var vitality = GetAbilityModifier(AbilityType.Vitality, target);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical) +
                Stat.GetDefense(target, CombatDamageType.Electrical);
            var damage = Combat.CalculateDamage(dmg, perception, vitality, defense, 0);

            if (Random.D100(1) <= knockdownChance)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, knockdownLength);
            }

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Electrical), target);
            });

            CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
            Enmity.ModifyEnmity(activator, target, 10);
        }

        private void ConcussionGrenade1()
        {
            _builder.Create(FeatType.ConcussionGrenade1, PerkType.ConcussionGrenade)
                .Name("Concussion Grenade I")
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
                    var vfx = EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst_Silent);
                    vfx = EffectLinkEffects(vfx, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake));
                    ExplosiveImpact(activator, location, vfx, "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 3f, 0, 0f);
                    });
                });
        }

        private void ConcussionGrenade2()
        {
            _builder.Create(FeatType.ConcussionGrenade2, PerkType.ConcussionGrenade)
                .Name("Concussion Grenade II")
                .HasRecastDelay(RecastGroup.Grenades, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    var vfx = EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst_Silent);
                    vfx = EffectLinkEffects(vfx, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake));
                    ExplosiveImpact(activator, location, vfx, "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 4.5f, 30, 30f);
                    });
                });
        }

        private void ConcussionGrenade3()
        {
            _builder.Create(FeatType.ConcussionGrenade3, PerkType.ConcussionGrenade)
                .Name("Concussion Grenade III")
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
                    var vfx = EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst_Silent);
                    vfx = EffectLinkEffects(vfx, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake));
                    ExplosiveImpact(activator, location, vfx, "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 7.5f, 50, 60f);
                    });
                });
        }
    }
}
