using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.OneHanded
{
    public class HackingBladeAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public HackingBladeAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            HackingBlade1(builder);
            HackingBlade2(builder);
            HackingBlade3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlotType.RightHand, activator);
            var rightHandType = GetBaseItemType(weapon);

            if (ItemService.VibrobladeBaseItemTypes.Contains(rightHandType))
            {
                return string.Empty;

            }
            else
                return "A vibroblade must be equipped in your right hand to use this ability.";
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {


            int dmg;
            int dc;

            switch (level)
            {
                default:
                case 1:
                    dmg = 6;
                    dc = 10;
                    break;
                case 2:
                    dmg = 15;
                    dc = 15;
                    break;
                case 3:
                    dmg = 22;
                    dc = 20;
                    break;
            }

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.OneHanded);

            CombatPointService.AddCombatPoint(activator, target, SkillType.OneHanded, 3);

            var attackerStat = GetAbilityScore(activator, AbilityType.Might);
            var attack = StatService.GetAttack(activator, AbilityType.Might, SkillType.OneHanded);
            var defense = StatService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = CombatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);

            dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Will, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);

            if (checkResult == SavingThrowResultType.Failed)
            {
            }
            
            EnmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void HackingBlade1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.HackingBlade1, PerkType.HackingBlade)
                .Name("Hacking Blade I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.HackingBlade, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void HackingBlade2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.HackingBlade2, PerkType.HackingBlade)
                .Name("Hacking Blade II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.HackingBlade, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void HackingBlade3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.HackingBlade3, PerkType.HackingBlade)
                .Name("Hacking Blade III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.HackingBlade, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
