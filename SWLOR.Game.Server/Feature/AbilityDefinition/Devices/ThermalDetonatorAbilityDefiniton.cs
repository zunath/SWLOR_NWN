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
    public class ThermalDetonatorAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            ThermalDetonator1();
            ThermalDetonator2();
            ThermalDetonator3();

            return _builder.Build();
        }

        private void Impact(uint activator, uint target, int dmg)
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

       

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), target);
            });

            CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
            Enmity.ModifyEnmity(activator, target, 180);
        }

        private void ThermalDetonator1()
        {
            _builder.Create(FeatType.ThermalDetonator1, PerkType.ThermalDetonator)
                .Name("Thermal Detonator I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ThermalDetonator, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(6)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    var vfx = EffectVisualEffect(VisualEffect.Fnf_Fireball);
                    vfx = EffectLinkEffects(vfx, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake));
                    ExplosiveImpact(activator, location, vfx, "explosion1", RadiusSize.Large, (target) =>
                    {

                        var perceptionbonus = GetAbilityModifier(AbilityType.Perception, activator);
                        var permultiplier5 = perceptionbonus * 5;
                        var bonusdamage5 = 20 + permultiplier5;
                        Impact(activator, target, bonusdamage5);
                    });
                });
        }

        private void ThermalDetonator2()
        {
            _builder.Create(FeatType.ThermalDetonator2, PerkType.ThermalDetonator)
                .Name("Thermal Detonator II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ThermalDetonator, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(8)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    var vfx = EffectVisualEffect(VisualEffect.Fnf_Fireball);
                    vfx = EffectLinkEffects(vfx, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake));
                    ExplosiveImpact(activator, location, vfx, "explosion1", RadiusSize.Large, (target) =>
                    {    
                        var perceptionbonus = GetAbilityModifier(AbilityType.Perception, activator);
                        var permultiplier15 = perceptionbonus * 10;
                        var bonusdamage15 = 40 + permultiplier15;
                        Impact(activator, target, bonusdamage15);
                    });
                });
        }

        private void ThermalDetonator3()
        {
            _builder.Create(FeatType.ThermalDetonator3, PerkType.ThermalDetonator)
                .Name("Thermal Detonator III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ThermalDetonator, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(10)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    var vfx = EffectVisualEffect(VisualEffect.Fnf_Fireball);
                    vfx = EffectLinkEffects(vfx, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake));
                    ExplosiveImpact(activator, location, vfx, "explosion1", RadiusSize.Large, (target) =>
                    {
                        var perceptionbonus = GetAbilityModifier(AbilityType.Perception, activator);
                        var permultiplier15 = perceptionbonus * 15;
                        var bonusdamage15 = 60 + permultiplier15;
                        Impact(activator, target, bonusdamage15);
                    });
                });
        }
    }
}
