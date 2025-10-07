using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.Beasts
{
    public class ClipAbilityDefinition: IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public ClipAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
        {
            _serviceProvider = serviceProvider;
            _statCalculation = statCalculation;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Clip1(builder);
            Clip2(builder);
            Clip3(builder);
            Clip4(builder);
            Clip5(builder);

            return builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg, int dc)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Perception) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Perception) / 2;

            var totalStat = beastmasterStat + beastStat;
            var attack = _statCalculation.CalculateAttack(activator, AbilityType.Perception, SkillType.Invalid);
            var defense = _statCalculation.CalculateDefense(target);
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
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Starburst_Green), target);
            });

            const float Duration = 3f;
            dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Fortitude, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectStunned(), target, Duration);
                AbilityService.ApplyTemporaryImmunity(target, Duration, ImmunityType.Stun);
            }

            EnmityService.ModifyEnmity(activator, target, 250 + damage);
        }

        private void Clip1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Clip1, PerkType.Clip)
                .Name("Clip I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.Clip, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 10, 8);
                });
        }
        private void Clip2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Clip2, PerkType.Clip)
                .Name("Clip II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.Clip, 60f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 12, 10);
                });
        }
        private void Clip3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Clip3, PerkType.Clip)
                .Name("Clip III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.Clip, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 14, 12);
                });
        }
        private void Clip4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Clip4, PerkType.Clip)
                .Name("Clip IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.Clip, 60f)
                .RequirementStamina(6)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 16, 14);
                });
        }
        private void Clip5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Clip5, PerkType.Clip)
                .Name("Clip V")
                .Level(5)
                .HasRecastDelay(RecastGroupType.Clip, 60f)
                .RequirementStamina(7)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 18, 16);
                });
        }

    }
}
