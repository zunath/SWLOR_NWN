//using Random = SWLOR.Game.Server.Service.Random;

using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.TwoHanded
{
    public class SkewerAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public SkewerAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Skewer1(builder);
            Skewer2(builder);
            Skewer3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);

            if (!ItemService.PolearmBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a polearm ability.";
            }
            else
                return string.Empty;
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
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

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.TwoHanded);
            
            var attackerStat = GetAbilityModifier(AbilityType.Might, activator);
            var attack = StatService.GetAttack(activator, AbilityType.Might, SkillType.TwoHanded);
            var defense = StatService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = CombatService.CalculateDamage(
                attack, 
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);

            dc = CombatService.CalculateSavingThrowDC(activator, SavingThrow.Will, dc);
            var checkResult = WillSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                // TODO: Implement DequeueWeaponAbility method
                // AbilityService.DequeueWeaponAbility(target);
                AbilityService.EndConcentrationAbility(target);
                SendMessageToPC(activator, ColorToken.Gray(GetName(target)) + "'s  concentration has been broken.");
                SendMessageToPC(target, ColorToken.Gray(GetName(activator)) + " broke your concentration.");
            }

            CombatPointService.AddCombatPoint(activator, target, SkillType.TwoHanded, 3);
            EnmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void Skewer1(IAbilityBuilder builder)
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
        private void Skewer2(IAbilityBuilder builder)
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
        private void Skewer3(IAbilityBuilder builder)
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
