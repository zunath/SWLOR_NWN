//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ranged
{
    public class QuickDrawAbilityDefinition : IAbilityListDefinition
    {
        private readonly IItemService _itemService;
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;

        public QuickDrawAbilityDefinition(IItemService itemService, ICombatService combatService, IStatService statService)
        {
            _itemService = itemService;
            _combatService = combatService;
            _statService = statService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            QuickDraw1(builder);
            QuickDraw2(builder);
            QuickDraw3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!_itemService.PistolBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a pistol ability.";
            }
            else
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
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

            dmg += _combatService.GetAbilityDamageBonus(activator, SkillType.Ranged);


            var attackerStat = _combatService.GetPerkAdjustedAbilityScore(activator);
            var attack = _statService.GetAttack(activator, AbilityType.Perception, SkillType.Ranged);
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
            AssignCommand(activator, () => ActionPlayAnimation(Animation.QuickDraw));

            CombatPoint.AddCombatPoint(activator, target, SkillType.Ranged, 3);
            Enmity.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private static void QuickDraw1(AbilityBuilder builder)
        {
            builder.Create(FeatType.QuickDraw1, PerkType.QuickDraw)
                .Name("Quick Draw I")
                .Level(1)
                .HasRecastDelay(RecastGroup.QuickDraw, 30f)
                .HasMaxRange(30.0f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void QuickDraw2(AbilityBuilder builder)
        {
            builder.Create(FeatType.QuickDraw2, PerkType.QuickDraw)
                .Name("Quick Draw II")
                .Level(2)
                .HasRecastDelay(RecastGroup.QuickDraw, 30f)
                .HasMaxRange(30.0f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void QuickDraw3(AbilityBuilder builder)
        {
            builder.Create(FeatType.QuickDraw3, PerkType.QuickDraw)
                .Name("Quick Draw III")
                .Level(3)
                .HasRecastDelay(RecastGroup.QuickDraw, 30f)
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