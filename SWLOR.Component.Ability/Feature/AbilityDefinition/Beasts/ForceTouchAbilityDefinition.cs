using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beasts
{
    public class ForceTouchAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public ForceTouchAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceTouch1(builder);
            ForceTouch2(builder);
            ForceTouch3(builder);
            ForceTouch4(builder);
            ForceTouch5(builder);

            return builder.Build();
        }

        private void ImpactAction(uint activator, uint target, int dmg)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Willpower) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Willpower) / 2;

            var totalStat = beastmasterStat + beastStat;
            var attack = StatService.GetAttack(activator, AbilityType.Willpower, SkillType.Invalid);
            var defense = StatService.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
            var defenderStat = GetAbilityScore(target, AbilityType.Willpower);

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
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Head_Mind), target);
            });

            EnmityService.ModifyEnmity(activator, target, 250 + damage);
        }

        private void ForceTouch1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceTouch1, PerkType.ForceTouch)
                .Name("Force Touch I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceTouch, 30f)
                .RequirementFP(3)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 12);
                });
        }

        private void ForceTouch2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceTouch2, PerkType.ForceTouch)
                .Name("Force Touch II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceTouch, 30f)
                .RequirementFP(4)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 16);
                });
        }

        private void ForceTouch3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceTouch3, PerkType.ForceTouch)
                .Name("Force Touch III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForceTouch, 30f)
                .RequirementFP(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 20);
                });
        }

        private void ForceTouch4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceTouch4, PerkType.ForceTouch)
                .Name("Force Touch IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.ForceTouch, 30f)
                .RequirementFP(6)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 24);
                });
        }

        private void ForceTouch5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceTouch5, PerkType.ForceTouch)
                .Name("Force Touch V")
                .Level(5)
                .HasRecastDelay(RecastGroup.ForceTouch, 30f)
                .RequirementFP(7)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, target, 28);
                });
        }


    }
}
