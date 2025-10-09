using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.Ranged
{
    public class DoubleShotAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public DoubleShotAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
        {
            _serviceProvider = serviceProvider;
            _statCalculation = statCalculation;
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();

        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            DoubleShot1(builder);
            DoubleShot2(builder);
            DoubleShot3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlotType.RightHand, activator);

            if (!ItemService.PistolBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a pistol ability.";
            }
            else
                return string.Empty;
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;

            switch (level)
            {
                case 1:
                    dmg = 8;
                    break;
                case 2:
                    dmg = 18;
                    break;
                case 3:
                    dmg = 28;
                    break;
                default:
                    break;
            }

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.Ranged);

            CombatPointService.AddCombatPoint(activator, target, SkillType.Ranged, 3);

            // First attack
            var attackerStat = CombatService.GetPerkAdjustedAbilityScore(activator);
            var attack = _statCalculation.CalculateAttack(activator, AbilityType.Perception, SkillType.Ranged);
            var defense = _statCalculation.CalculateDefense(target);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = CombatService.CalculateDamage(
                attack, 
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);

            // Second attack
            damage = CombatService.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
            AssignCommand(activator, () => ActionPlayAnimation(AnimationType.DoubleShot));
            AssignCommand(activator, () => ActionPlayAnimation(AnimationType.DoubleShot));

            EnmityService.ModifyEnmity(activator, target, 200 * level + damage);
        }

        private void DoubleShot1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleShot1, PerkType.DoubleShot)
                .Name("Double Shot I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.DoubleShot, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
        private void DoubleShot2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleShot2, PerkType.DoubleShot)
                .Name("Double Shot II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.DoubleShot, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
        private void DoubleShot3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleShot3, PerkType.DoubleShot)
                .Name("Double Shot III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.DoubleShot, 60f)
                .RequirementStamina(8)
                .IsWeaponAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
    }
}

