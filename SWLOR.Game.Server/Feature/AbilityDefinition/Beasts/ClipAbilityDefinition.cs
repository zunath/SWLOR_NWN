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
    public class ClipAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Clip1();
            Clip2();
            Clip3();
            Clip4();
            Clip5();

            return _builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Perception) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Perception) / 2;

            var totalStat = beastmasterStat + beastStat;
            var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Invalid);
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
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Starburst_Green), target);
            });

            const float Duration = 3f;
            StatusEffect.ApplyStatusEffect(activator, target, typeof(StunnedStatusEffect), Duration, damageType);

            Enmity.ModifyEnmity(activator, target, 250 + damage);
        }

        private void Clip1()
        {
            _builder.Create(FeatType.Clip1, PerkType.Clip)
                .Name("Clip I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Clip, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 10);
                });
        }
        private void Clip2()
        {
            _builder.Create(FeatType.Clip2, PerkType.Clip)
                .Name("Clip II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Clip, 60f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 12);
                });
        }
        private void Clip3()
        {
            _builder.Create(FeatType.Clip3, PerkType.Clip)
                .Name("Clip III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Clip, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 14);
                });
        }
        private void Clip4()
        {
            _builder.Create(FeatType.Clip4, PerkType.Clip)
                .Name("Clip IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.Clip, 60f)
                .RequirementStamina(6)
                .IsWeaponAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 16);
                });
        }
        private void Clip5()
        {
            _builder.Create(FeatType.Clip5, PerkType.Clip)
                .Name("Clip V")
                .Level(5)
                .HasRecastDelay(RecastGroup.Clip, 60f)
                .RequirementStamina(7)
                .IsWeaponAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 18);
                });
        }
    }
}
