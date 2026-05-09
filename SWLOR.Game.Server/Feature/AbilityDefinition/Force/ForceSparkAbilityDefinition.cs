using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

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
        private void Impact(uint activator, uint target, int dmg, int tier, Type statusEffect, int dc)
        {
            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
            var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
            var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
            var defense = Stat.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
            dmg += (attackerStat * ((tier - 1) / 2)) + attackerStat;
            var damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);


            dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Fortitude, dc, AbilityType.Willpower);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);

            if (checkResult == SavingThrowResultType.Failed)
            {
                StatusEffect.ApplyStatusEffect(activator, target, statusEffect, 60f);
            }

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Starburst_Red), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Beam_Silent_Lightning, false, 2f), target);

            if (Stat.GetCurrentFP(activator) < 2 + (tier))
            {
                var darkBargain = 7 * ((2 + tier - Stat.GetCurrentFP(activator)));
                Stat.ReduceFP(activator, Stat.GetCurrentFP(activator));
                ApplyEffectToObject(DurationType.Instant, EffectDamage(darkBargain), activator);
            }
            else { Stat.ReduceFP(activator, 2 + tier); }

            Enmity.ModifyEnmity(activator, target, 150 + damage);
            CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);
        }

        private void ForceSpark1()
        {
            _builder.Create(FeatType.ForceSpark1, PerkType.ForceSpark)
                .Name("Force Spark I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceSpark, 6f)
                .HasActivationDelay(2f)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 0, 1, typeof(ForceSpark1StatusEffect), 8);
                });
        }

        private void ForceSpark2()
        {
            _builder.Create(FeatType.ForceSpark2, PerkType.ForceSpark)
                .Name("Force Spark II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceSpark, 6f)
                .HasActivationDelay(2f)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 15, 2, typeof(ForceSpark2StatusEffect), 12);
                });
        }

        private void ForceSpark3()
        {
            _builder.Create(FeatType.ForceSpark3, PerkType.ForceSpark)
                .Name("Force Spark III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForceSpark, 6f)
                .HasActivationDelay(2f)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 30, 3, typeof(ForceSpark3StatusEffect), 14);
                });
        }
    }
}
