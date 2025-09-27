using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.OneHanded
{
    public class PoisonStabAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public PoisonStabAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private IStatusEffectService StatusEffectService => _serviceProvider.GetRequiredService<IStatusEffectService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            PoisonStab1(builder);
            PoisonStab2(builder);
            PoisonStab3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlotType.RightHand, activator);
            var rightHandType = GetBaseItemType(weapon);

            if (ItemService.FinesseVibrobladeBaseItemTypes.Contains(rightHandType))
            {
                return string.Empty;
            }
            else
                return "A finesse vibroblade must be equipped in your right hand to use this ability.";
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {


            int dmg;
            int dc;

            switch (level)
            {
                default:
                case 1:
                    dmg = 8;
                    dc = 10;
                    break;
                case 2:
                    dmg = 18;
                    dc = 15;
                    break;
                case 3:
                    dmg = 28;
                    dc = 20;
                    break;
            }

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.OneHanded);

            CombatPointService.AddCombatPoint(activator, target, SkillType.OneHanded, 3);

            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var attack = StatService.GetAttack(activator, AbilityType.Perception, SkillType.OneHanded);
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

            dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Fortitude, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                StatusEffectService.Apply(activator, target, StatusEffectType.Poison, 60f);
            }

            EnmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void PoisonStab1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonStab1, PerkType.PoisonStab)
                .Name("Poison Stab I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.PoisonStab, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void PoisonStab2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonStab2, PerkType.PoisonStab)
                .Name("Poison Stab II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.PoisonStab, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void PoisonStab3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.PoisonStab3, PerkType.PoisonStab)
                .Name("Poison Stab III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.PoisonStab, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
