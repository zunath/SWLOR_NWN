using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.Beasts
{
    public class AssaultAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public AssaultAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
        {
            _serviceProvider = serviceProvider;
            _statCalculation = statCalculation;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Assault1(builder);
            Assault2(builder);
            Assault3(builder);
            Assault4(builder);
            Assault5(builder);

            return builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Agility) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Agility) / 2;

            var totalStat = beastmasterStat + beastStat;
            var attack = _statCalculation.CalculateAttack(activator, AbilityType.Agility, SkillType.Invalid);
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
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Com_Blood_Spark_Medium), target);
            });

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Magblue), activator);

            EnmityService.ModifyEnmity(activator, target, 350 + damage);
        }

        private void Assault1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Assault1, PerkType.Assault)
                .Name("Assault I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.Assault, 60f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 10);
                });
        }
        private void Assault2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Assault2, PerkType.Assault)
                .Name("Assault II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.Assault, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 14);
                });
        }
        private void Assault3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Assault3, PerkType.Assault)
                .Name("Assault III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.Assault, 60f)
                .RequirementStamina(6)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 16);
                });
        }
        private void Assault4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Assault4, PerkType.Assault)
                .Name("Assault IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.Assault, 60f)
                .RequirementStamina(7)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 22);
                });
        }
        private void Assault5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Assault5, PerkType.Assault)
                .Name("Assault V")
                .Level(5)
                .HasRecastDelay(RecastGroupType.Assault, 60f)
                .RequirementStamina(8)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 26);
                });
        }

    }
}
