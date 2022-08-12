using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
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
        
        private void Impact(uint activator, uint target, int dmg, int knockdownChance, float knockdownLength)
        {
            if (GetFactionEqual(activator, target))
                return;

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Devices);

            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Devices);
            var damage = Combat.CalculateDamage(
                attack,
                dmg,
                attackerStat, 
                defense, 
                defenderStat, 
                0);

            if (Random.D100(1) <= knockdownChance)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, knockdownLength);
            }

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Electrical), target);
            });

            CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
            Enmity.ModifyEnmity(activator, target, 180);
        }

        private void ConcussionGrenade1()
        {
            _builder.Create(FeatType.ConcussionGrenade1, PerkType.ConcussionGrenade)
                .Name("Concussion Grenade I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Grenades, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    var vfx = EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst_Silent);
                    vfx = EffectLinkEffects(vfx, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake));
                    ExplosiveImpact(activator, location, vfx, "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 6, 0, 0f);
                    });
                });
        }

        private void ConcussionGrenade2()
        {
            _builder.Create(FeatType.ConcussionGrenade2, PerkType.ConcussionGrenade)
                .Name("Concussion Grenade II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Grenades, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    var vfx = EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst_Silent);
                    vfx = EffectLinkEffects(vfx, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake));
                    ExplosiveImpact(activator, location, vfx, "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 10, 30, 6f);
                    });
                });
        }

        private void ConcussionGrenade3()
        {
            _builder.Create(FeatType.ConcussionGrenade3, PerkType.ConcussionGrenade)
                .Name("Concussion Grenade III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Grenades, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    var vfx = EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst_Silent);
                    vfx = EffectLinkEffects(vfx, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake));
                    ExplosiveImpact(activator, location, vfx, "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 16, 50, 8f);
                    });
                });
        }
    }
}
