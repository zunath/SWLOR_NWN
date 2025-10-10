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

namespace SWLOR.Component.Ability.Definitions.Beasts
{
    public class ShockingSlashAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public ShockingSlashAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
        {
            _serviceProvider = serviceProvider;
            _statCalculation = statCalculation;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatCalculationService CombatCalculationService => _serviceProvider.GetRequiredService<ICombatCalculationService>();

        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ShockingSlash1(builder);
            ShockingSlash2(builder);
            ShockingSlash3(builder);
            ShockingSlash4(builder);
            ShockingSlash5(builder);

            return builder.Build();
        }

        private void Impact(uint activator, Location targetLocation, int dmg, int dc, int level)
        {
            var baseDC = dc;
            const float ConeSize = 10f;

            AssignCommand(activator, () =>
            {
                ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Electric_Explosion), targetLocation);
            });

            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Might) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Might) / 2;
            var totalStat = beastStat + beastmasterStat;

            var attack = _statCalculation.CalculateAttack(activator, AbilityType.Might, SkillType.Invalid);
            var eVFX = EffectVisualEffect(VisualEffectType.Vfx_Imp_Head_Electricity);

            var target = GetFirstObjectInShape(ShapeType.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            while (GetIsObjectValid(target))
            {
                if (target != activator)
                {
                    var defense = _statCalculation.CalculateDefense(target);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = CombatCalculationService.CalculateAbilityDamage(
                        activator,
                        target,
                        dmg,
                        CombatDamageType.Physical,
                        SkillType.Invalid,
                        AbilityType.Might,
                        AbilityType.Vitality
                    );

                    var eDMG = EffectDamage(damage, DamageType.Electrical);
                    EnmityService.ModifyEnmity(activator, target, 220);

                    // Copying the target is needed because the variable gets adjusted outside the scope of the internal lambda.
                    var targetCopy = target;
                    DelayCommand(0.1f, () =>
                    {
                        AssignCommand(activator, () =>
                        {
                            ApplyEffectToObject(DurationType.Instant, eDMG, targetCopy);
                            ApplyEffectToObject(DurationType.Instant, eVFX, targetCopy);
                        });

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

        private void ShockingSlash1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ShockingSlash1, PerkType.ShockingSlash)
                .Name("Shocking Slash I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ShockingSlash, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator,_, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 8, -1, level);
                });
        }
        private void ShockingSlash2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ShockingSlash2, PerkType.ShockingSlash)
                .Name("Shocking Slash II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ShockingSlash, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 12, -1, level);
                });
        }
        private void ShockingSlash3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ShockingSlash3, PerkType.ShockingSlash)
                .Name("Shocking Slash III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.ShockingSlash, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 16, 8, level);
                });
        }
        private void ShockingSlash4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ShockingSlash4, PerkType.ShockingSlash)
                .Name("Shocking Slash IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.ShockingSlash, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(7)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 20, 12, level);
                });
        }
        private void ShockingSlash5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ShockingSlash5, PerkType.ShockingSlash)
                .Name("Shocking Slash V")
                .Level(5)
                .HasRecastDelay(RecastGroupType.ShockingSlash, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, level, targetLocation) =>
                {
                    Impact(activator, targetLocation, 24, 14, level);
                });
        }
    }
}

