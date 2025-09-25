//using Random = SWLOR.Game.Server.Service.Random;

using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.TwoHanded
{
    public class DoubleThrustAbilityDefinition : IAbilityListDefinition
    {
        private readonly IItemService _itemService;
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public DoubleThrustAbilityDefinition(IItemService itemService, ICombatService combatService, IStatService statService, ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _itemService = itemService;
            _combatService = combatService;
            _statService = statService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            DoubleThrust1(builder);
            DoubleThrust2(builder);
            DoubleThrust3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!_itemService.PolearmBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a polearm ability.";
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
                    dmg = 11;
                    break;
                case 2:
                    dmg = 19;
                    break;
                case 3:
                    dmg = 29;
                    break;
                default:
                    break;
            }

            dmg += _combatService.GetAbilityDamageBonus(activator, SkillType.TwoHanded);

            var attackerStat = GetAbilityScore(activator, AbilityType.Might);
            var attack = _statService.GetAttack(activator, AbilityType.Might, SkillType.TwoHanded);
            var defense = _statService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = _combatService.CalculateDamage(
                attack, 
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);

            AssignCommand(activator, () => ActionPlayAnimation(Animation.DoubleThrust));

            _combatPointService.AddCombatPoint(activator, target, SkillType.TwoHanded, 3);
            _enmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void DoubleThrust1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleThrust1, PerkType.DoubleThrust)
                .Name("Double Thrust I")
                .Level(1)
                .HasRecastDelay(RecastGroup.DoubleThrust, 60f)
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
        private void DoubleThrust2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleThrust2, PerkType.DoubleThrust)
                .Name("Double Thrust II")
                .Level(2)
                .HasRecastDelay(RecastGroup.DoubleThrust, 60f)
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
        private void DoubleThrust3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleThrust3, PerkType.DoubleThrust)
                .Name("Double Thrust III")
                .Level(3)
                .HasRecastDelay(RecastGroup.DoubleThrust, 60f)
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
