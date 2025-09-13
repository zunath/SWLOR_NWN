using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ThrowRockAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ThrowRock1(builder);
            ThrowRock2(builder);
            ThrowRock3(builder);
            ThrowRock4(builder);
            ThrowRock5(builder);

            return builder.Build();
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;
            var willBonus = GetAbilityScore(activator, AbilityType.Willpower);
            var perBonus = GetAbilityScore(activator, AbilityType.Perception);

            switch (level)
            {
                case 1:
                    dmg = willBonus;
                    break;
                case 2:
                    dmg = 10 + willBonus * 3 / 2;
                    break;
                case 3:
                    dmg = 15 + willBonus * 2;
                    break;
                case 4:
                    dmg = 20 + willBonus * 5 / 2;
                    break;
                case 5:
                    dmg = 25 + willBonus * 3;
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Force);

            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
            var damage = Combat.CalculateDamage(
                attack,
                dmg,
                attackerStat,
                defense,
                defenderStat,
                0);
            var delay = GetDistanceBetweenLocations(GetLocation(activator), targetLocation) / 25.0f;

            AssignCommand(activator, () => DoThrowRock(target, level));

            var eDMG = EffectDamage(damage, DamageType.Bludgeoning);
            var eVFX = EffectVisualEffect(VisualEffect.Vfx_Imp_Dust_Explosion);

            Enmity.ModifyEnmity(activator, target, damage);
            CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);

            DelayCommand(delay, () =>
            {
                ApplyEffectToObject(DurationType.Instant, eVFX, target);
                ApplyEffectToObject(DurationType.Instant, eDMG, target);
            });
        }

        private static void ThrowRock1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowRock1, PerkType.ThrowRock)
                .Name("Throw Rock I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ThrowRock, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementFP(1)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating(VisualEffect.None)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasImpactAction(ImpactAction);
        }

        private static void ThrowRock2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowRock2, PerkType.ThrowRock)
                .Name("Throw Rock II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ThrowRock, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementFP(2)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating(VisualEffect.None)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasImpactAction(ImpactAction);
        }

        private static void ThrowRock3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowRock3, PerkType.ThrowRock)
                .Name("Throw Rock III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ThrowRock, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementFP(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating(VisualEffect.None)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasImpactAction(ImpactAction);
        }

        private static void ThrowRock4(AbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowRock4, PerkType.ThrowRock)
                .Name("Throw Rock IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.ThrowRock, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementFP(4)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating(VisualEffect.None)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasImpactAction(ImpactAction);
        }
        private static void ThrowRock5(AbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowRock5, PerkType.ThrowRock)
                .Name("Throw Rock V")
                .Level(5)
                .HasRecastDelay(RecastGroup.ThrowRock, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(40.0f)
                .RequirementFP(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating(VisualEffect.None)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasImpactAction(ImpactAction);
        }
        private static void DoThrowRock(uint target, int level)
        {
            VisualEffect rockType = VisualEffect.Vfx_Imp_Mirv_Rock;
            if(level >= 3 && level < 5) { rockType = VisualEffect.Vfx_Imp_Mirv_Rock3; }
            else if(level == 5) { rockType = VisualEffect.Vfx_Imp_Mirv_Rock2;  }

            var missile = EffectVisualEffect(rockType);
            ApplyEffectToObject(DurationType.Instant, missile, target);
        }
    }
}