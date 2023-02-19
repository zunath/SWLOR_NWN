using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

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

        private void ImpactAction(uint activator, uint target, int dmg, int dc)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Perception) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Perception) / 2;

            var totalStat = beastmasterStat + beastStat;
            var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Invalid);
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
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Starburst_Green), target);

            const float Duration = 3f;
            dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Fortitude, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectStunned(), target, Duration);
                Ability.ApplyTemporaryImmunity(target, Duration, ImmunityType.Stun);
            }

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
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 10, 8);
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
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 12, 10);
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
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 14, 12);
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
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 16, 14);
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
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 18, 16);
                });
        }

    }
}
