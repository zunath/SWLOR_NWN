using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Devices
{
    public class FlamethrowerAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public FlamethrowerAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private IStatusEffectService StatusEffectService => _serviceProvider.GetRequiredService<IStatusEffectService>();

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
            var attack = StatService.GetAttack(activator, AbilityType.Perception, SkillType.Devices);
            var eVFX = EffectVisualEffect(VisualEffectType.Vfx_Imp_Flame_S);

            var target = GetFirstObjectInShape(ShapeType.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            while (GetIsObjectValid(target))
            {
                if (target != activator)
                {
                    var defense = StatService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = CombatService.CalculateDamage(
                        attack,
                        dmg,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    var eDMG = EffectDamage(damage, DamageType.Fire);
                    EnmityService.ModifyEnmity(activator, target, 280);
                    CombatPointService.AddCombatPoint(activator, target, SkillType.Devices, 3);
                    
                    // Copying the target is needed because the variable gets adjusted outside the scope of the internal lambda.
                    var targetCopy = target;
                    DelayCommand(0.1f, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, eDMG, targetCopy);
                        ApplyEffectToObject(DurationType.Instant, eVFX, targetCopy);

                        dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Reflex, baseDC);
                        var checkResult = ReflexSave(targetCopy, dc, SavingThrowType.None, activator);
                        if (checkResult == SavingThrowResultType.Failed)
                        {
                            StatusEffectService.Apply(activator, targetCopy, StatusEffectType.Burn, 30f);
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
                .HasRecastDelay(RecastGroup.Flamethrower, 12f)
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
                .HasRecastDelay(RecastGroup.Flamethrower, 12f)
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
                .HasRecastDelay(RecastGroup.Flamethrower, 12f)
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
