using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class BiteAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Bite1();
            Bite2();
            Bite3();
            Bite4();
            Bite5();

            return _builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityModifier(AbilityType.Might, beastmaster) / 2;
            var beastStat = GetAbilityModifier(AbilityType.Might, activator) / 2;

            var totalStat = beastmasterStat + beastStat;
            var attack = Stat.GetAttack(activator, AbilityType.Might, SkillType.Invalid);
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
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Sonic), target);

            Enmity.ModifyEnmity(activator, target, 250 + damage);
        }

        private void Bite1()
        {
            _builder.Create(FeatType.Bite1, PerkType.Bite)
                .Name("Bite I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Bite, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 12);
                });
        }

        private void Bite2()
        {
            _builder.Create(FeatType.Bite2, PerkType.Bite)
                .Name("Bite II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Bite, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 16);
                });
        }

        private void Bite3()
        {
            _builder.Create(FeatType.Bite3, PerkType.Bite)
                .Name("Bite III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Bite, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 20);
                });
        }

        private void Bite4()
        {
            _builder.Create(FeatType.Bite4, PerkType.Bite)
                .Name("Bite IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.Bite, 30f)
                .RequirementStamina(6)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 24);
                });
        }

        private void Bite5()
        {
            _builder.Create(FeatType.Bite5, PerkType.Bite)
                .Name("Bite V")
                .Level(5)
                .HasRecastDelay(RecastGroup.Bite, 30f)
                .RequirementStamina(7)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 28);
                });
        }


    }
}
