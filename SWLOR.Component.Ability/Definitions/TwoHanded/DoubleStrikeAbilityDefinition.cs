//using Random = SWLOR.Game.Server.Service.Random;

using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.TwoHanded
{
    public class DoubleStrikeAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public DoubleStrikeAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
        {
            _serviceProvider = serviceProvider;
            _statCalculation = statCalculation;
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            DoubleStrike1(builder);
            DoubleStrike2(builder);
            DoubleStrike3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlotType.RightHand, activator);

            if (!ItemService.SaberstaffBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a saberstaff ability.";
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
                    dmg = 12;
                    break;
                case 2:
                    dmg = 21;
                    break;
                case 3:
                    dmg = 29;
                    break;
                default:
                    break;
            }

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.TwoHanded);

            var stat = AbilityType.Perception;
            if (AbilityService.IsAbilityToggled(activator, AbilityToggleType.StrongStyleSaberstaff))
            {
                stat = AbilityType.Might;
            }

            var attackerStat = CombatService.GetPerkAdjustedAbilityScore(activator);
            var attack = _statCalculation.CalculateAttack(activator, stat, SkillType.TwoHanded);
            var defense = _statCalculation.CalculateDefense(target);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = CombatService.CalculateDamage(
                attack, 
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Sonic), target);

            AssignCommand(activator, () => ActionPlayAnimation(AnimationType.DoubleStrike));

            CombatPointService.AddCombatPoint(activator, target, SkillType.TwoHanded, 3);
            EnmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void DoubleStrike1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleStrike1, PerkType.DoubleStrike)
                .Name("Double Strike I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.DoubleStrike, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
        private void DoubleStrike2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleStrike2, PerkType.DoubleStrike)
                .Name("Double Strike II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.DoubleStrike, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
        private void DoubleStrike3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleStrike3, PerkType.DoubleStrike)
                .Name("Double Strike III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.DoubleStrike, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ImpactAction(activator, target, level, targetLocation);
                    ImpactAction(activator, target, level, targetLocation);
                });
        }
    }
}
