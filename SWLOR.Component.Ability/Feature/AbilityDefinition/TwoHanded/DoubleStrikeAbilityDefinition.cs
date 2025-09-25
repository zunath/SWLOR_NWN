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
    public class DoubleStrikeAbilityDefinition : IAbilityListDefinition
    {
        private readonly IItemService _itemService;
        private readonly IAbilityService _abilityService;
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public DoubleStrikeAbilityDefinition(IItemService itemService, IAbilityService abilityService, ICombatService combatService, IStatService statService, ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _itemService = itemService;
            _abilityService = abilityService;
            _combatService = combatService;
            _statService = statService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            DoubleStrike1(builder);
            DoubleStrike2(builder);
            DoubleStrike3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!_itemService.SaberstaffBaseItemTypes.Contains(GetBaseItemType(weapon)))
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

            dmg += _combatService.GetAbilityDamageBonus(activator, SkillType.TwoHanded);

            var stat = AbilityType.Perception;
            if (_abilityService.IsAbilityToggled(activator, AbilityToggleType.StrongStyleSaberstaff))
            {
                stat = AbilityType.Might;
            }

            var attackerStat = _combatService.GetPerkAdjustedAbilityScore(activator);
            var attack = _statService.GetAttack(activator, stat, SkillType.TwoHanded);
            var defense = _statService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = _combatService.CalculateDamage(
                attack, 
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Sonic), target);

            AssignCommand(activator, () => ActionPlayAnimation(Animation.DoubleStrike));

            _combatPointService.AddCombatPoint(activator, target, SkillType.TwoHanded, 3);
            _enmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void DoubleStrike1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleStrike1, PerkType.DoubleStrike)
                .Name("Double Strike I")
                .Level(1)
                .HasRecastDelay(RecastGroup.DoubleStrike, 60f)
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
                .HasRecastDelay(RecastGroup.DoubleStrike, 60f)
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
                .HasRecastDelay(RecastGroup.DoubleStrike, 60f)
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
