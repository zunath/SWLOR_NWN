using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.Devices
{
    public class FlamethrowerAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public FlamethrowerAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
        {
            _serviceProvider = serviceProvider;
            _statCalculation = statCalculation;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatCalculationService CombatCalculationService => _serviceProvider.GetRequiredService<ICombatCalculationService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Flamethrower1(builder);
            Flamethrower2(builder);
            Flamethrower3(builder);

            return builder.Build();
        }

        private void Impact(uint activator, Location targetLocation, int dmg, int dc)
        {
            var baseDC = dc;
            const float ConeSize = 10f;

            AssignCommand(activator, () =>
            {
                ActionPlayAnimation(AnimationType.CastOutAnimation, 1f, 2.1f);
                ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffectType.Vfx_Flamethrower), activator, 2f);
            });

            var attackerStat = GetAbilityScore( activator, AbilityType.Perception);
            var attack = _statCalculation.CalculateAttack(activator, AbilityType.Perception, SkillType.Devices);
            var eVFX = EffectVisualEffect(VisualEffectType.Vfx_Imp_Flame_S);

            var target = GetFirstObjectInShape(ShapeType.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            while (GetIsObjectValid(target))
            {
                if (target != activator)
                {
                    var defense = _statCalculation.CalculateDefense(target);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = CombatCalculationService.CalculateAbilityDamage(activator, target, dmg, CombatDamageType.Physical, SkillType.Devices, AbilityType.Perception, AbilityType.Vitality);

                    var eDMG = EffectDamage(damage, DamageType.Fire);
                    EnmityService.ModifyEnmity(activator, target, 280);
                    CombatPointService.AddCombatPoint(activator, target, SkillType.Devices, 3);
                    
                    // Copying the target is needed because the variable gets adjusted outside the scope of the internal lambda.
                    var targetCopy = target;
                    DelayCommand(0.1f, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, eDMG, targetCopy);
                        ApplyEffectToObject(DurationType.Instant, eVFX, targetCopy);

                        dc += _statCalculation.CalculateSavingThrow(activator, SavingThrowCategoryType.Reflex);
                        var checkResult = ReflexSave(targetCopy, dc, SavingThrowType.None, activator);
                        if (checkResult == SavingThrowResultType.Failed)
                        {
                        }
                    });
                }

                target = GetNextObjectInShape(ShapeType.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            }
        }

        private void Flamethrower1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Flamethrower1, PerkType.Flamethrower)
                .Name("Flamethrower I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.Flamethrower, 12f)
                .HasActivationDelay(2f)
                .RequirementStamina(1)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                    Impact(activator, targetLocation, perBonus, -1);
                });
        }

        private void Flamethrower2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Flamethrower2, PerkType.Flamethrower)
                .Name("Flamethrower II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.Flamethrower, 12f)
                .HasActivationDelay(2f)
                .RequirementStamina(2)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                    int perDMG = 20 + (perBonus * 3 / 2);
                    Impact(activator, targetLocation, perDMG, 8);
                });
        }

        private void Flamethrower3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Flamethrower3, PerkType.Flamethrower)
                .Name("Flamethrower III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.Flamethrower, 12f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var perBonus = GetAbilityScore(activator, AbilityType.Perception);
                    var perDMG = 40 + (perBonus * 2);
                    Impact(activator, targetLocation, perDMG, 12);
                });
        }
    }
}


