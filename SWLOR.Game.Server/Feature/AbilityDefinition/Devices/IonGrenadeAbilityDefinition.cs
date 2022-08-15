﻿using System.Collections.Generic;
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
    public class IonGrenadeAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            IonGrenade1();
            IonGrenade2();
            IonGrenade3();

            return _builder.Build();
        }
        
        private void Impact(uint activator, uint target, int dmg, int stunChance)
        {
            if (GetFactionEqual(activator, target))
                return;

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Devices);

            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Devices);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);

            if (GetRacialType(target) == RacialType.Robot && Random.D100(1) <= stunChance)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectStunned(), target, 6f);
            }

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Electrical), target);
            });

            CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
            Enmity.ModifyEnmity(activator, target, 350);
        }

        private void IonGrenade1()
        {
            _builder.Create(FeatType.IonGrenade1, PerkType.IonGrenade)
                .Name("Ion Grenade I")
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
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 4, 0);
                    });
                });
        }

        private void IonGrenade2()
        {
            _builder.Create(FeatType.IonGrenade2, PerkType.IonGrenade)
                .Name("Ion Grenade II")
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
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 8, 50);
                    });
                });
        }

        private void IonGrenade3()
        {
            _builder.Create(FeatType.IonGrenade3, PerkType.IonGrenade)
                .Name("Ion Grenade III")
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
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 14, 70);
                    });
                });
        }
    }
}
