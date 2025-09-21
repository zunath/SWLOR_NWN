//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityServicex;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded
{
    public class CircleSlashAbilityDefinition : IAbilityListDefinition
    {
        private readonly IItemService _itemService;
        private readonly IAbilityService _abilityService;
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public CircleSlashAbilityDefinition(IItemService itemService, IAbilityService abilityService, ICombatService combatService, IStatService statService, ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _itemService = itemService;
            _abilityService = abilityService;
            _combatService = combatService;
            _statService = statService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            CircleSlash1(builder);
            CircleSlash2(builder);
            CircleSlash3(builder);

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
                    dmg = 10;
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

            dmg += _combatService.GetAbilityDamageBonus(activator, SkillType.TwoHanded);

            var stat = AbilityType.Perception;
            if (_abilityService.IsAbilityToggled(activator, AbilityToggleType.StrongStyleSaberstaff))
            {
                stat = AbilityType.Might;
            }

            var count = 0;
            var creature = GetFirstObjectInShape(Shape.Sphere, RadiusSize.Large, GetLocation(activator), true);
            while (GetIsObjectValid(creature) && count < 3)
            {
                if (GetIsReactionTypeHostile(creature, activator))
                {
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

                    var dTarget = creature;

                    DelayCommand(0.1f, () =>
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), dTarget));

                    _combatPointService.AddCombatPoint(activator, creature, SkillType.TwoHanded, 3);
                    _enmityService.ModifyEnmity(activator, creature, 100 * level + damage);
                    count++;
                }

                creature = GetNextObjectInShape(Shape.Sphere, RadiusSize.Large, GetLocation(activator), true);
            }

            AssignCommand(activator, () => ActionPlayAnimation(Animation.Whirlwind));
        }

        private void CircleSlash1(AbilityBuilder builder)
        {
            builder.Create(FeatType.CircleSlash1, PerkType.CircleSlash)
                .Name("Circle Slash I")
                .Level(1)
                .HasRecastDelay(RecastGroup.CircleSlash, 30f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void CircleSlash2(AbilityBuilder builder)
        {
            builder.Create(FeatType.CircleSlash2, PerkType.CircleSlash)
                .Name("Circle Slash II")
                .Level(2)
                .HasRecastDelay(RecastGroup.CircleSlash, 30f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void CircleSlash3(AbilityBuilder builder)
        {
            builder.Create(FeatType.CircleSlash3, PerkType.CircleSlash)
                .Name("Circle Slash III")
                .Level(3)
                .HasRecastDelay(RecastGroup.CircleSlash, 30f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
