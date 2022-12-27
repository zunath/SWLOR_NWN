using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class FragGrenadeAbilityDefinition: ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            FragGrenade1();
            FragGrenade2();
            FragGrenade3();

            return _builder.Build();
        }
        
        private void Impact(uint activator, uint target, int dmg, int dc, float bleedLength)
        {
            if (GetFactionEqual(activator, target))
                return;

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Devices);

            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Devices);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);

            if (dc > 0)
            {
                var checkResult = ReflexSave(target, dc, SavingThrowType.None, activator);
                if (checkResult == SavingThrowResultType.Failed)
                {
                    StatusEffect.Apply(activator, target, StatusEffectType.Bleed, bleedLength);
                }
            }

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), target);
            });

            CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
            Enmity.ModifyEnmity(activator, target, 320);
        }

        private void FragGrenade1()
        {
            _builder.Create(FeatType.FragGrenade1, PerkType.FragGrenade)
                .Name("Frag Grenade I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Grenades, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(2)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Fnf_Fireball), "explosion2", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 6, -1, 0f);
                    });
                });
        }

        private void FragGrenade2()
        {
            _builder.Create(FeatType.FragGrenade2, PerkType.FragGrenade)
                .Name("Frag Grenade II")
                .Level(2)
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
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Fnf_Fireball), "explosion2", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 10, 6, 30f);
                    });
                });
        }

        private void FragGrenade3()
        {
            _builder.Create(FeatType.FragGrenade3, PerkType.FragGrenade)
                .Name("Frag Grenade III")
                .Level(3)
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
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Fnf_Fireball), "explosion2", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 16, 10, 60f);
                    });
                });
        }
    }
}
