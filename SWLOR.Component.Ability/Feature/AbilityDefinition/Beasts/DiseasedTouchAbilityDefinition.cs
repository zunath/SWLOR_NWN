using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beasts
{
    public class DiseasedTouchAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public DiseasedTouchAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            DiseasedTouch1(builder);
            DiseasedTouch2(builder);
            DiseasedTouch3(builder);
            DiseasedTouch4(builder);
            DiseasedTouch5(builder);

            return builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg, int dc, int level)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Perception) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Perception) / 2;

            var totalStat = beastmasterStat + beastStat;
            var attack = StatService.GetAttack(activator, AbilityType.Perception, SkillType.Invalid);
            var defense = StatService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);

            var damage = CombatService.CalculateDamage(
                attack,
                dmg,
                totalStat,
                defense,
                defenderStat,
                0
            );

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Disease_S), target);
            });

            dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Fortitude, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
            }

            EnmityService.ModifyEnmity(activator, target, 250 + damage);
        }

        private void DiseasedTouch1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.DiseasedTouch1, PerkType.DiseasedTouch)
                .Name("Diseased Touch I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.DiseasedTouch, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 8, 8, level);
                });
        }
        private void DiseasedTouch2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.DiseasedTouch2, PerkType.DiseasedTouch)
                .Name("Diseased Touch II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.DiseasedTouch, 60f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 11, 10, level);
                });
        }
        private void DiseasedTouch3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.DiseasedTouch3, PerkType.DiseasedTouch)
                .Name("Diseased Touch III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.DiseasedTouch, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 14, 12, level);
                });
        }
        private void DiseasedTouch4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.DiseasedTouch4, PerkType.DiseasedTouch)
                .Name("Diseased Touch IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.DiseasedTouch, 60f)
                .RequirementStamina(6)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 17, 14, level);
                });
        }
        private void DiseasedTouch5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.DiseasedTouch5, PerkType.DiseasedTouch)
                .Name("Diseased Touch V")
                .Level(5)
                .HasRecastDelay(RecastGroupType.DiseasedTouch, 60f)
                .RequirementStamina(7)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 20, 16, level);
                });
        }

    }
}
