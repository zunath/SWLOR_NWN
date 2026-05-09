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
    public class DisturbanceAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Disturbance1();
            Disturbance2();
            Disturbance3();

            return _builder.Build();
        }

        private void Impact(uint activator, uint target, int dmg, int tier, Type statusEffect, int dc)
        {
            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
            var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
            var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
            var defense = Stat.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
            dmg += (attackerStat * ((tier -1) / 2)) + attackerStat;
            var damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);

            dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Will, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);

            if (checkResult == SavingThrowResultType.Failed)
            {
                StatusEffect.ApplyStatusEffect(activator, target, statusEffect, 60f);
            }

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Starburst_Green), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Pulse_Holy), target);

            Enmity.ModifyEnmityOnAll(activator, 150 + damage);
            CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);
        }

        private void Disturbance1()
        {
            _builder.Create(FeatType.Disturbance1, PerkType.Disturbance)
                .Name("Disturbance I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Disturbance, 6f)
                .HasActivationDelay(2f)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 0, 1, typeof(Disturbance1StatusEffect), 8);
                });
        }

        private void Disturbance2()
        {
            _builder.Create(FeatType.Disturbance2, PerkType.Disturbance)
                .Name("Disturbance II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Disturbance, 6f)
                .HasActivationDelay(2f)
                .RequirementFP(1)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 15, 2, typeof(Disturbance2StatusEffect), 12);
                });
        }

        private void Disturbance3()
        {
            _builder.Create(FeatType.Disturbance3, PerkType.Disturbance)
                .Name("Disturbance III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Disturbance, 6f)
                .HasActivationDelay(2f)
                .RequirementFP(2)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 30, 3, typeof(Disturbance3StatusEffect), 14);
                });
        }
    }
}
