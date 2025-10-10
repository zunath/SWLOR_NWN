//using Random = SWLOR.Game.Server.Service.Random;

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

namespace SWLOR.Component.Ability.Definitions.Ranged
{
    public class QuickDrawAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public QuickDrawAbilityDefinition(
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
            QuickDraw1(builder);
            QuickDraw2(builder);
            QuickDraw3(builder);

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
                    dmg = 10;
                    break;
                case 2:
                    dmg = 20;
                    break;
                case 3:
                    dmg = 30;
                    break;
                default:
                    break;
            }

            var damage = CombatCalculationService.CalculateAbilityDamage(
                activator,
                target,
                dmg,
                CombatDamageType.Physical,
                SkillType.Ranged,
                AbilityType.Perception,
                AbilityType.Vitality
            );
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
            AssignCommand(activator, () => ActionPlayAnimation(AnimationType.QuickDraw));

            CombatPointService.AddCombatPoint(activator, target, SkillType.Ranged, 3);
            EnmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void QuickDraw1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.QuickDraw1, PerkType.QuickDraw)
                .Name("Quick Draw I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.QuickDraw, 30f)
                .HasMaxRange(30.0f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void QuickDraw2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.QuickDraw2, PerkType.QuickDraw)
                .Name("Quick Draw II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.QuickDraw, 30f)
                .HasMaxRange(30.0f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void QuickDraw3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.QuickDraw3, PerkType.QuickDraw)
                .Name("Quick Draw III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.QuickDraw, 30f)
                .HasMaxRange(30.0f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}

