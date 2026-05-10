using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class ClawAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Claw1();
            Claw2();
            Claw3();
            Claw4();
            Claw5();

            return _builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg, int level)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Might) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Might) / 2;

            var totalStat = beastmasterStat + beastStat;
            var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.Invalid);
            var damageType = CombatDamageType.Physical;
            var defense = Stat.GetDefense(target, damageType, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);

            var damage = Combat.CalculateDamage(
                attack,
                dmg,
                totalStat,
                defense,
                defenderStat,
                0
            );
            damage = Resistance.ApplyResistanceToDamage(target, damageType, damage);

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, damageType.GetNWScriptDamageType()), target);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Blood_Spark_Small), target);
            });

            StatusEffect.ApplyStatusEffect(activator, target, typeof(BleedStatusEffect), 30f, damageType);

            Enmity.ModifyEnmity(activator, target, 250 + damage);
        }

        private void Claw1()
        {
            _builder.Create(FeatType.Claw1, PerkType.Claw)
                .Name("Claw I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Claw, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 8, level);
                });
        }
        private void Claw2()
        {
            _builder.Create(FeatType.Claw2, PerkType.Claw)
                .Name("Claw II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Claw, 60f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 11, level);
                });
        }
        private void Claw3()
        {
            _builder.Create(FeatType.Claw3, PerkType.Claw)
                .Name("Claw III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Claw, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 14, level);
                });
        }
        private void Claw4()
        {
            _builder.Create(FeatType.Claw4, PerkType.Claw)
                .Name("Claw IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.Claw, 60f)
                .RequirementStamina(6)
                .IsWeaponAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 17, level);
                });
        }
        private void Claw5()
        {
            _builder.Create(FeatType.Claw5, PerkType.Claw)
                .Name("Claw V")
                .Level(5)
                .HasRecastDelay(RecastGroup.Claw, 60f)
                .RequirementStamina(7)
                .IsWeaponAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 20, level);
                });
        }
    }
}
