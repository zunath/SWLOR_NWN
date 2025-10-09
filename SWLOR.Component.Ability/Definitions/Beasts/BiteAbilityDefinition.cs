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
    public class BiteAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public BiteAbilityDefinition(
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
            Bite1(builder);
            Bite2(builder);
            Bite3(builder);
            Bite4(builder);
            Bite5(builder);

            return builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityModifier(AbilityType.Might, beastmaster) / 2;
            var beastStat = GetAbilityModifier(AbilityType.Might, activator) / 2;

            var totalStat = beastmasterStat + beastStat;
            var attack = _statCalculation.CalculateAttack(activator, AbilityType.Might, SkillType.Invalid);
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
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Head_Sonic), target);
            });

            EnmityService.ModifyEnmity(activator, target, 250 + damage);
        }

        private void Bite1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Bite1, PerkType.Bite)
                .Name("Bite I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.Bite, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 12);
                });
        }

        private void Bite2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Bite2, PerkType.Bite)
                .Name("Bite II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.Bite, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 16);
                });
        }

        private void Bite3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Bite3, PerkType.Bite)
                .Name("Bite III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.Bite, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 20);
                });
        }

        private void Bite4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Bite4, PerkType.Bite)
                .Name("Bite IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.Bite, 30f)
                .RequirementStamina(6)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 24);
                });
        }

        private void Bite5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Bite5, PerkType.Bite)
                .Name("Bite V")
                .Level(5)
                .HasRecastDelay(RecastGroupType.Bite, 30f)
                .RequirementStamina(7)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 28);
                });
        }


    }
}
