using SWLOR.Game.Server.Service.AbilityService;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class FlameBreathAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            FlameBreath1();
            FlameBreath2();
            FlameBreath3();
            FlameBreath4();
            FlameBreath5();

            return _builder.Build();
        }

        private void Impact(uint activator, Location targetLocation, int dmg, bool appliesBurn, int level)
        {
            const float ConeSize = 10f;

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Flamethrower), activator, 2f);
            });

            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Might) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Might) / 2;
            var totalStat = beastStat + beastmasterStat;

            var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.Invalid);
            var eVFX = EffectVisualEffect(VisualEffect.Vfx_Imp_Flame_S);

            var target = GetFirstObjectInShape(Shape.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            while (GetIsObjectValid(target))
            {
                if (target != activator)
                {
                    var damageType = CombatDamageType.Fire;
                    var defense = Stat.GetDefense(target, damageType, AbilityType.Vitality);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = Combat.CalculateDamage(
                        attack,
                        dmg,
                        totalStat,
                        defense,
                        defenderStat,
                        0);
                    damage = Resistance.ApplyResistanceToDamage(target, damageType, damage);

                    var eDMG = EffectDamage(damage, DamageType.Fire);
                    Enmity.ModifyEnmity(activator, target, 220);

                    // Copying the target is needed because the variable gets adjusted outside the scope of the internal lambda.
                    var targetCopy = target;
                    DelayCommand(0.1f, () =>
                    {
                        AssignCommand(activator, () =>
                        {
                            ApplyEffectToObject(DurationType.Instant, eDMG, targetCopy);
                            ApplyEffectToObject(DurationType.Instant, eVFX, targetCopy);
                        });

                        if (appliesBurn)
                        {
                            StatusEffect.ApplyStatusEffect(activator, targetCopy, new BurnStatusEffect(level), 30f, damageType);
                        }
                    });
                }

                target = GetNextObjectInShape(Shape.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            }
        }

        private void FlameBreath1()
        {
            _builder.Create(FeatType.FlameBreath1, PerkType.FlameBreath)
                .Name("Flame Breath I")
                .Level(1)
                .HasRecastDelay(RecastGroup.FlameBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 8, false, level);
                });
        }
        private void FlameBreath2()
        {
            _builder.Create(FeatType.FlameBreath2, PerkType.FlameBreath)
                .Name("Flame Breath II")
                .Level(2)
                .HasRecastDelay(RecastGroup.FlameBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 12, false, level);
                });
        }
        private void FlameBreath3()
        {
            _builder.Create(FeatType.FlameBreath3, PerkType.FlameBreath)
                .Name("Flame Breath III")
                .Level(3)
                .HasRecastDelay(RecastGroup.FlameBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 16, true, level);
                });
        }
        private void FlameBreath4()
        {
            _builder.Create(FeatType.FlameBreath4, PerkType.FlameBreath)
                .Name("Flame Breath IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.FlameBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(7)
                .IsCastedAbility()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 20, true, level);
                });
        }
        private void FlameBreath5()
        {
            _builder.Create(FeatType.FlameBreath5, PerkType.FlameBreath)
                .Name("Flame Breath V")
                .Level(5)
                .HasRecastDelay(RecastGroup.FlameBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 24, true, level);
                });
        }
    }
}
