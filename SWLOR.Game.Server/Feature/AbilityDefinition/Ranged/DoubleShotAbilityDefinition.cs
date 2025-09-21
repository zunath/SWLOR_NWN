using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ranged
{
    public class DoubleShotAbilityDefinition : IAbilityListDefinition
    {
        private readonly IItemService _itemService;
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;

        public DoubleShotAbilityDefinition(IItemService itemService, ICombatService combatService, IStatService statService)
        {
            _itemService = itemService;
            _combatService = combatService;
            _statService = statService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            DoubleShot1(builder);
            DoubleShot2(builder);
            DoubleShot3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!_itemService.PistolBaseItemTypes.Contains(GetBaseItemType(weapon)))
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

            dmg += _combatService.GetAbilityDamageBonus(activator, SkillType.Ranged);

            var combatPointService = ServiceContainer.GetService<ICombatPointService>();
            combatPointService.AddCombatPoint(activator, target, SkillType.Ranged, 3);

            // First attack
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

            // Second attack
            damage = CombatService.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
            AssignCommand(activator, () => ActionPlayAnimation(Animation.DoubleShot));
            AssignCommand(activator, () => ActionPlayAnimation(Animation.DoubleShot));

            Enmity.ModifyEnmity(activator, target, 200 * level + damage);
        }

        private void DoubleShot1(AbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleShot1, PerkType.DoubleShot)
                .Name("Double Shot I")
                .Level(1)
                .HasRecastDelay(RecastGroup.DoubleShot, 60f)
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
        private void DoubleShot2(AbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleShot2, PerkType.DoubleShot)
                .Name("Double Shot II")
                .Level(2)
                .HasRecastDelay(RecastGroup.DoubleShot, 60f)
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
        private void DoubleShot3(AbilityBuilder builder)
        {
            builder.Create(FeatType.DoubleShot3, PerkType.DoubleShot)
                .Name("Double Shot III")
                .Level(3)
                .HasRecastDelay(RecastGroup.DoubleShot, 60f)
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