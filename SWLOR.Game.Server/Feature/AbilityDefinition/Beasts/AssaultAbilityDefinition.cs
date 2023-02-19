using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class AssaultAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Assault1();
            Assault2();
            Assault3();
            Assault4();
            Assault5();

            return _builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Agility) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Agility) / 2;

            var totalStat = beastmasterStat + beastStat;
            var attack = Stat.GetAttack(activator, AbilityType.Agility, SkillType.Invalid);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);

            var damage = Combat.CalculateDamage(
                attack,
                dmg,
                totalStat,
                defense,
                defenderStat,
                0
            );

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Blood_Spark_Medium), target);

            StatusEffect.Apply(activator, activator, StatusEffectType.Assault, 30f);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Magblue), activator);

            Enmity.ModifyEnmity(activator, target, 350 + damage);
        }

        private void Assault1()
        {
            _builder.Create(FeatType.Assault1, PerkType.Assault)
                .Name("Assault I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Assault, 60f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 10);
                });
        }
        private void Assault2()
        {
            _builder.Create(FeatType.Assault2, PerkType.Assault)
                .Name("Assault II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Assault, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 14);
                });
        }
        private void Assault3()
        {
            _builder.Create(FeatType.Assault3, PerkType.Assault)
                .Name("Assault III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Assault, 60f)
                .RequirementStamina(6)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 16);
                });
        }
        private void Assault4()
        {
            _builder.Create(FeatType.Assault4, PerkType.Assault)
                .Name("Assault IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.Assault, 60f)
                .RequirementStamina(7)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 22);
                });
        }
        private void Assault5()
        {
            _builder.Create(FeatType.Assault5, PerkType.Assault)
                .Name("Assault V")
                .Level(5)
                .HasRecastDelay(RecastGroup.Assault, 60f)
                .RequirementStamina(8)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 26);
                });
        }

    }
}
