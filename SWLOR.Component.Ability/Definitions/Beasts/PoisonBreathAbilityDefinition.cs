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
    public class PoisonBreathAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public PoisonBreathAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
        {
            _serviceProvider = serviceProvider;
            _statCalculation = statCalculation;
        }

        // Lazy-loaded services to break circular dependencies
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            PoisonBreath1(builder);
            PoisonBreath2(builder);
            PoisonBreath3(builder);
            PoisonBreath4(builder);
            PoisonBreath5(builder);

            return builder.Build();
        }

        private void Impact(uint activator, Location targetLocation, int dmg, int dc)
        {
            var baseDC = dc;
            const float ConeSize = 10f;

            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityScore(beastmaster, AbilityType.Perception) / 2;
            var beastStat = GetAbilityScore(activator, AbilityType.Perception) / 2;
            var totalStat = beastStat + beastmasterStat;

            var attack = _statCalculation.CalculateAttack(activator, AbilityType.Perception, SkillType.Invalid);
            var eVFX = EffectVisualEffect(VisualEffectType.Vfx_Imp_Poison_S);

            var target = GetFirstObjectInShape(ShapeType.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            while (GetIsObjectValid(target))
            {
                if (target != activator)
                {
                    var defense = _statCalculation.CalculateDefense(target);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = CombatService.CalculateDamage(
                        attack,
                        dmg,
                        totalStat,
                        defense,
                        defenderStat,
                        0);

                    var eDMG = EffectDamage(damage, DamageType.Acid);
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

                        dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Reflex, baseDC);
                        var checkResult = ReflexSave(targetCopy, dc, SavingThrowType.None, activator);
                        if (checkResult == SavingThrowResultType.Failed)
                        {
                        }
                    });
                }

                target = GetNextObjectInShape(ShapeType.SpellCone, ConeSize, targetLocation, true, ObjectType.Creature);
            }
        }

        private void PoisonBreath1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonBreath1, PerkType.PoisonBreath)
                .Name("Poison Breath I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.PoisonBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    Impact(activator, targetLocation, 8, -1);
                });
        }
        private void PoisonBreath2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonBreath2, PerkType.PoisonBreath)
                .Name("Poison Breath II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.PoisonBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    Impact(activator, targetLocation, 12, -1);
                });
        }
        private void PoisonBreath3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonBreath3, PerkType.PoisonBreath)
                .Name("Poison Breath III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.PoisonBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    Impact(activator, targetLocation, 16, 8);
                });
        }
        private void PoisonBreath4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonBreath4, PerkType.PoisonBreath)
                .Name("Poison Breath IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.PoisonBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(7)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    Impact(activator, targetLocation, 20, 12);
                });
        }
        private void PoisonBreath5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonBreath5, PerkType.PoisonBreath)
                .Name("Poison Breath V")
                .Level(5)
                .HasRecastDelay(RecastGroupType.PoisonBreath, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    Impact(activator, targetLocation, 24, 14);
                });
        }
    }
}
