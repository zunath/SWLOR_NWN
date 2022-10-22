using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceSparkAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            ForceSpark1();
            ForceSpark2();
            ForceSpark3();

            return _builder.Build();
        }
        private void Impact(uint activator, uint target, int dmg, int evaDecrease)
        {
            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
            var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
            var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
            var defense = Stat.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
            var damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);


         

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
            ApplyEffectToObject(DurationType.Temporary, EffectAttackDecrease(evaDecrease), target, 10f);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Starburst_Red), target);

            Enmity.ModifyEnmity(activator, target, 300 + damage);
            CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);
        }

        private void ForceSpark1()
        {
            _builder.Create(FeatType.ForceSpark1, PerkType.ForceSpark)
                .Name("Force Spark I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceSpark, 20f)
                .RequirementFP(3)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var willmod = GetAbilityModifier(AbilityType.Willpower, activator);
                    var damage1 = (willmod * 1) + 9;
                    Impact(activator, target, damage1, 2);
                });
        }

        private void ForceSpark2()
        {
            _builder.Create(FeatType.ForceSpark2, PerkType.ForceSpark)
                .Name("Force Spark II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceSpark, 20f)
                .RequirementFP(5)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var willmod = GetAbilityModifier(AbilityType.Willpower, activator);
                    var damage2 = (willmod * 3) + 19;
                    Impact(activator, target, damage2, 4);
                });
        }

        private void ForceSpark3()
        {
            _builder.Create(FeatType.ForceSpark3, PerkType.ForceSpark)
                .Name("Force Spark III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForceSpark, 20f)
                .RequirementFP(7)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var willmod = GetAbilityModifier(AbilityType.Willpower, activator);
                    var damage3 = (willmod * 5) + 32;
                    Impact(activator, target, damage3, 6);
                });
        }
    }
}
