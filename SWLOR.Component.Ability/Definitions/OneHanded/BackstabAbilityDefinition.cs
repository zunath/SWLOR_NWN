using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.OneHanded
{
    public class BackstabAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public BackstabAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
        {
            _serviceProvider = serviceProvider;
            _statCalculation = statCalculation;
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private ICombatCalculationService CombatCalculationService => _serviceProvider.GetRequiredService<ICombatCalculationService>();

        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Backstab1(builder);
            Backstab2(builder);
            Backstab3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlotType.RightHand, activator);
            var rightHandType = GetBaseItemType(weapon);

            if (ItemService.FinesseVibrobladeBaseItemTypes.Contains(rightHandType))
            {
                return string.Empty;
            }
            else
                return "A finesse vibroblade must be equipped in your right hand to use this ability.";
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var dmg = 0;



            switch (level)
            {
                case 1:
                    dmg = 14;
                    break;
                case 2:
                    dmg = 30;
                    break;
                case 3:
                    dmg = 45;
                    break;
                default:
                    break;
            }

            if (abs((int)(GetFacing(activator) - GetFacing(target))) > 200f ||
                abs((int)(GetFacing(activator) - GetFacing(target))) < 160f ||
                GetDistanceBetween(activator, target) > 5f)
            {
                dmg /= 2;
            }

            CombatPointService.AddCombatPoint(activator, target, SkillType.OneHanded, 3);

            var damage = CombatCalculationService.CalculateAbilityDamage(
                activator,
                target,
                dmg,
                CombatDamageType.Physical,
                SkillType.OneHanded,
                AbilityType.Perception,
                AbilityType.Vitality
            );
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);

            AssignCommand(activator, () => ActionPlayAnimation(AnimationType.Backstab));
            EnmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void Backstab1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Backstab1, PerkType.Backstab)
                .Name("Backstab I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.Backstab, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void Backstab2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Backstab2, PerkType.Backstab)
                .Name("Backstab II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.Backstab, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void Backstab3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Backstab3, PerkType.Backstab)
                .Name("Backstab III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.Backstab, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}

