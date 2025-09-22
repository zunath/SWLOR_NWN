using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class ThrowRockAbilityDefinition : IAbilityListDefinition
    {
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public ThrowRockAbilityDefinition(ICombatService combatService, IStatService statService, ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _combatService = combatService;
            _statService = statService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ThrowRock1(builder);
            ThrowRock2(builder);
            ThrowRock3(builder);
            ThrowRock4(builder);
            ThrowRock5(builder);

            return builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
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

            dmg += _combatService.GetAbilityDamageBonus(activator, SkillType.Force);

            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
            var defense = _statService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var attack = _statService.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
            var damage = _combatService.CalculateDamage(
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

            _enmityService.ModifyEnmity(activator, target, damage);
            _combatPointService.AddCombatPoint(activator, target, SkillType.Force, 3);

            DelayCommand(delay, () =>
            {
                ApplyEffectToObject(DurationType.Instant, eVFX, target);
                ApplyEffectToObject(DurationType.Instant, eDMG, target);
            });
        }

        private void ThrowRock1(IAbilityBuilder builder)
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

        private void ThrowRock2(IAbilityBuilder builder)
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

        private void ThrowRock3(IAbilityBuilder builder)
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

        private void ThrowRock4(IAbilityBuilder builder)
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
        private void ThrowRock5(IAbilityBuilder builder)
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
