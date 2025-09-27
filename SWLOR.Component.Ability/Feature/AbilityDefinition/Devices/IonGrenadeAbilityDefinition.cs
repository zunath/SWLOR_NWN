using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Constants;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Devices
{
    public class IonGrenadeAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        public IonGrenadeAbilityDefinition(IServiceProvider serviceProvider) 
            : base(serviceProvider)
        {
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            IonGrenade1(builder);
            IonGrenade2(builder);
            IonGrenade3(builder);

            return builder.Build();
        }
        
        private void Impact(uint activator, uint target, int dmg, int dc)
        {
            if (GetFactionEqual(activator, target))
                return;

            const float Duration = 6f;
            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.Devices);

            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var attack = StatService.GetAttack(activator, AbilityType.Perception, SkillType.Devices);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var defense = StatService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var damage = CombatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);

            var race = GetRacialType(target);
            if (dc > 0 &&
                (race == RacialType.Robot ||
                race == RacialType.Droid ||
                race == RacialType.Cyborg))
            {
                dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Fortitude, dc);
                var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
                if (checkResult == SavingThrowResultType.Failed)
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectStunned(), target, Duration);
                    AbilityService.ApplyTemporaryImmunity(target, Duration, ImmunityType.Stun);
                }
            }

            DelayCommand(0f, () =>
            {
                AssignCommand(activator, () =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Electrical), target);
                });
            });

            CombatPointService.AddCombatPoint(activator, target, SkillType.Devices, 3);
            EnmityService.ModifyEnmity(activator, target, 350);
        }

        private void IonGrenade1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.IonGrenade1, PerkType.IonGrenade)
                .Name("Ion Grenade I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.IonGrenade, 12f)
                .HasActivationDelay(2f)
                .RequirementStamina(1)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Electric_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                        var race = GetRacialType(target);
                        if (race == RacialType.Robot || race == RacialType.Droid || race == RacialType.Cyborg)
                        {
                            perBonus *= 3 / 2;
                        }
                        Impact(activator, target, perBonus, -1);
                    });
                });
        }

        private void IonGrenade2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.IonGrenade2, PerkType.IonGrenade)
                .Name("Ion Grenade II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.IonGrenade, 12f)
                .HasActivationDelay(2f)
                .RequirementStamina(2)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Electric_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                        var race = GetRacialType(target);
                        if (race == RacialType.Robot || race == RacialType.Droid || race == RacialType.Cyborg)
                        {
                            perBonus *= 3 / 2;
                        }
                        var perDMG = 15 + (perBonus * 3 / 2);
                        Impact(activator, target, perDMG, 10);
                    });
                });
        }

        private void IonGrenade3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.IonGrenade3, PerkType.IonGrenade)
                .Name("Ion Grenade III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.IonGrenade, 12f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Electric_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                        var race = GetRacialType(target);
                        if (race == RacialType.Robot || race == RacialType.Droid || race == RacialType.Cyborg)
                        {
                            perBonus *= 3 / 2;
                        }
                        var perDMG = 30 + (perBonus * 2);
                        Impact(activator, target, perDMG, 14);
                    });
                });
        }
    }
}
