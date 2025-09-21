//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Service;

using SWLOR.Shared.Core.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Models;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded
{
    public class SkewerAbilityDefinition : IAbilityListDefinition
    {
        private readonly IItemService _itemService;
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly IAbilityService _abilityService;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public SkewerAbilityDefinition(IItemService itemService, ICombatService combatService, IStatService statService, IAbilityService abilityService, ICombatPointService combatPointService, IEnmityService enmityService)
        {
            _itemService = itemService;
            _combatService = combatService;
            _statService = statService;
            _abilityService = abilityService;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            Skewer1(builder);
            Skewer2(builder);
            Skewer3(builder);

            return builder.Build();
        }

        private static string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!_itemService.PolearmBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a polearm ability.";
            }
            else
                return string.Empty;
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {

            int dmg;
            int dc;

            switch (level)
            {
                default:
                case 1:
                    dmg = 12;
                    dc = 10;
                    break;
                case 2:
                    dmg = 21;
                    dc = 15;
                    break;
                case 3:
                    dmg = 34;
                    dc = 20;
                    break;
            }

            dmg += _combatService.GetAbilityDamageBonus(activator, SkillType.TwoHanded);
            
            var attackerStat = GetAbilityModifier(AbilityType.Might, activator);
            var attack = _statService.GetAttack(activator, AbilityType.Might, SkillType.TwoHanded);
            var defense = _statService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = _combatService.CalculateDamage(
                attack, 
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);

            dc = _combatService.CalculateSavingThrowDC(activator, dc, 0, 0);
            var checkResult = WillSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                UsePerkFeat.DequeueWeaponAbility(target);
                _abilityService.EndConcentrationAbility(target);
                SendMessageToPC(activator, ColorToken.Gray(GetName(target)) + "'s  concentration has been broken.");
                SendMessageToPC(target, ColorToken.Gray(GetName(activator)) + " broke your concentration.");
            }

            _combatPointService.AddCombatPoint(activator, target, SkillType.TwoHanded, 3);
            _enmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private static void Skewer1(AbilityBuilder builder)
        {
            builder.Create(FeatType.Skewer1, PerkType.Skewer)
                .Name("Skewer I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Skewer, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void Skewer2(AbilityBuilder builder)
        {
            builder.Create(FeatType.Skewer2, PerkType.Skewer)
                .Name("Skewer II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Skewer, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private static void Skewer3(AbilityBuilder builder)
        {
            builder.Create(FeatType.Skewer3, PerkType.Skewer)
                .Name("Skewer III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Skewer, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
