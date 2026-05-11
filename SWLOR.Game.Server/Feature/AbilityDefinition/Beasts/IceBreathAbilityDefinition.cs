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
    public class IceBreathAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            IceBreath1();
            IceBreath2();
            IceBreath3();

            return _builder.Build();
        }

        private void Impact(uint activator, Location targetLocation, int dmg, bool appliesFreezing, int level)
        {
            const float ConeSize = 10f;

            AssignCommand(activator, () =>
            {
                ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Icestorm), targetLocation);
            });

            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Might) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Might) / 2;
            var totalStat = beastStat + beastmasterStat;

            var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.Invalid);

            var target = GetFirstObjectInShape(Shape.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            while (GetIsObjectValid(target))
            {
                if (target != activator)
                {
                    var damageType = CombatDamageType.Ice;
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

                    var eDMG = EffectDamage(damage, DamageType.Cold);
                    Enmity.ModifyEnmity(activator, target, 220);

                    // Copying the target is needed because the variable gets adjusted outside the scope of the internal lambda.
                    var targetCopy = target;
                    DelayCommand(0.1f, () =>
                    {
                        AssignCommand(activator, () =>
                        {
                            ApplyEffectToObject(DurationType.Instant, eDMG, targetCopy);
                        });

                        if (appliesFreezing)
                        {
                            StatusEffect.ApplyStatusEffect(activator, targetCopy, new FreezingStatusEffect(level), 30f, damageType);
                        }
                    });
                }

                target = GetNextObjectInShape(Shape.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            }
        }

        private void IceBreath1()
        {
            _builder.Create(FeatType.IceBreath1, PerkType.IceBreath)
                .Name("Ice Breath I")
                .Level(1)
                .HasRecastDelay(RecastGroup.IceBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 8, false, level);
                });
        }
        private void IceBreath2()
        {
            _builder.Create(FeatType.IceBreath2, PerkType.IceBreath)
                .Name("Ice Breath II")
                .Level(2)
                .HasRecastDelay(RecastGroup.IceBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 12, false, level);
                });
        }
        private void IceBreath3()
        {
            _builder.Create(FeatType.IceBreath3, PerkType.IceBreath)
                .Name("Ice Breath III")
                .Level(3)
                .HasRecastDelay(RecastGroup.IceBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 16, true, level);
                });
        }

    }
}
