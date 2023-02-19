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
    public class DiseasedTouchAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            DiseasedTouch1();
            DiseasedTouch2();
            DiseasedTouch3();
            DiseasedTouch4();
            DiseasedTouch5();

            return _builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg, int dc, int level)
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
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Disease_S), target);

            dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Fortitude, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                StatusEffect.Apply(activator, target, StatusEffectType.Disease, 30f, level);
            }

            Enmity.ModifyEnmity(activator, target, 250 + damage);
        }

        private void DiseasedTouch1()
        {
            _builder.Create(FeatType.DiseasedTouch1, PerkType.DiseasedTouch)
                .Name("Diseased Touch I")
                .Level(1)
                .HasRecastDelay(RecastGroup.DiseasedTouch, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 8, 8, level);
                });
        }
        private void DiseasedTouch2()
        {
            _builder.Create(FeatType.DiseasedTouch2, PerkType.DiseasedTouch)
                .Name("Diseased Touch II")
                .Level(2)
                .HasRecastDelay(RecastGroup.DiseasedTouch, 60f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 11, 10, level);
                });
        }
        private void DiseasedTouch3()
        {
            _builder.Create(FeatType.DiseasedTouch3, PerkType.DiseasedTouch)
                .Name("Diseased Touch III")
                .Level(3)
                .HasRecastDelay(RecastGroup.DiseasedTouch, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 14, 12, level);
                });
        }
        private void DiseasedTouch4()
        {
            _builder.Create(FeatType.DiseasedTouch4, PerkType.DiseasedTouch)
                .Name("Diseased Touch IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.DiseasedTouch, 60f)
                .RequirementStamina(6)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 17, 14, level);
                });
        }
        private void DiseasedTouch5()
        {
            _builder.Create(FeatType.DiseasedTouch5, PerkType.DiseasedTouch)
                .Name("Diseased Touch V")
                .Level(5)
                .HasRecastDelay(RecastGroup.DiseasedTouch, 60f)
                .RequirementStamina(7)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 20, 16, level);
                });
        }

    }
}
