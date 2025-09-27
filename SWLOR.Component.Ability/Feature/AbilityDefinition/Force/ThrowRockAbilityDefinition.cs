using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class ThrowRockAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public ThrowRockAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

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

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.Force);

            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
            var defense = StatService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var attack = StatService.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
            var damage = CombatService.CalculateDamage(
                attack,
                dmg,
                attackerStat,
                defense,
                defenderStat,
                0);
            var delay = GetDistanceBetweenLocations(GetLocation(activator), targetLocation) / 25.0f;

            AssignCommand(activator, () => DoThrowRock(target, level));

            var eDMG = EffectDamage(damage, DamageType.Bludgeoning);
            var eVFX = EffectVisualEffect(VisualEffectType.Vfx_Imp_Dust_Explosion);

            EnmityService.ModifyEnmity(activator, target, damage);
            CombatPointService.AddCombatPoint(activator, target, SkillType.Force, 3);

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
                .HasRecastDelay(RecastGroupType.ThrowRock, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementFP(1)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating(VisualEffectType.None)
                .UsesAnimation(AnimationType.CastOutAnimation)
                .HasImpactAction(ImpactAction);
        }

        private void ThrowRock2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowRock2, PerkType.ThrowRock)
                .Name("Throw Rock II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ThrowRock, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementFP(2)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating(VisualEffectType.None)
                .UsesAnimation(AnimationType.CastOutAnimation)
                .HasImpactAction(ImpactAction);
        }

        private void ThrowRock3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowRock3, PerkType.ThrowRock)
                .Name("Throw Rock III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.ThrowRock, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementFP(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating(VisualEffectType.None)
                .UsesAnimation(AnimationType.CastOutAnimation)
                .HasImpactAction(ImpactAction);
        }

        private void ThrowRock4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowRock4, PerkType.ThrowRock)
                .Name("Throw Rock IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.ThrowRock, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementFP(4)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating(VisualEffectType.None)
                .UsesAnimation(AnimationType.CastOutAnimation)
                .HasImpactAction(ImpactAction);
        }
        private void ThrowRock5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ThrowRock5, PerkType.ThrowRock)
                .Name("Throw Rock V")
                .Level(5)
                .HasRecastDelay(RecastGroupType.ThrowRock, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(40.0f)
                .RequirementFP(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .DisplaysVisualEffectWhenActivating(VisualEffectType.None)
                .UsesAnimation(AnimationType.CastOutAnimation)
                .HasImpactAction(ImpactAction);
        }
        private void DoThrowRock(uint target, int level)
        {
            VisualEffectType rockType = VisualEffectType.Vfx_Imp_Mirv_Rock;
            if(level >= 3 && level < 5) { rockType = VisualEffectType.Vfx_Imp_Mirv_Rock3; }
            else if(level == 5) { rockType = VisualEffectType.Vfx_Imp_Mirv_Rock2;  }

            var missile = EffectVisualEffect(rockType);
            ApplyEffectToObject(DurationType.Instant, missile, target);
        }
    }
}
