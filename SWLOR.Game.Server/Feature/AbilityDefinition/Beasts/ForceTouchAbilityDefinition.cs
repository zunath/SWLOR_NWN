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
    public class ForceTouchAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            ForceTouch1();
            ForceTouch2();
            ForceTouch3();
            ForceTouch4();
            ForceTouch5();

            return _builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Willpower) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Willpower) / 2;

            var totalStat = beastmasterStat + beastStat;
            var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Invalid);
            var defense = Stat.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
            var defenderStat = GetAbilityScore(target, AbilityType.Willpower);

            var damage = Combat.CalculateDamage(
                attack,
                dmg,
                totalStat,
                defense,
                defenderStat,
                0
            );
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Mind), target);

            Enmity.ModifyEnmity(activator, target, 250 + damage);
        }

        private void ForceTouch1()
        {
            _builder.Create(FeatType.ForceTouch1, PerkType.ForceTouch)
                .Name("Force Touch I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceTouch, 30f)
                .RequirementFP(3)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 12);
                });
        }

        private void ForceTouch2()
        {
            _builder.Create(FeatType.ForceTouch2, PerkType.ForceTouch)
                .Name("Force Touch II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceTouch, 30f)
                .RequirementFP(4)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 16);
                });
        }

        private void ForceTouch3()
        {
            _builder.Create(FeatType.ForceTouch3, PerkType.ForceTouch)
                .Name("Force Touch III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForceTouch, 30f)
                .RequirementFP(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 20);
                });
        }

        private void ForceTouch4()
        {
            _builder.Create(FeatType.ForceTouch4, PerkType.ForceTouch)
                .Name("Force Touch IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.ForceTouch, 30f)
                .RequirementFP(6)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 24);
                });
        }

        private void ForceTouch5()
        {
            _builder.Create(FeatType.ForceTouch5, PerkType.ForceTouch)
                .Name("Force Touch V")
                .Level(5)
                .HasRecastDelay(RecastGroup.ForceTouch, 30f)
                .RequirementFP(7)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 28);
                });
        }


    }
}
