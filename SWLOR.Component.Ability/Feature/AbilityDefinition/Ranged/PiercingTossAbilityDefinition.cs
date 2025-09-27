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

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Ranged
{
    public class PiercingTossAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public PiercingTossAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IStatusEffectService StatusEffectService => _serviceProvider.GetRequiredService<IStatusEffectService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            PiercingToss1(builder);
            PiercingToss2(builder);
            PiercingToss3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlotType.RightHand, activator);

            if (!ItemService.ThrowingWeaponBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a throwing ability.";
            }
            else
                return string.Empty;
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {

            int dmg;
            int dc;
            float duration;

            switch (level)
            {
                default:
                case 1:
                    dmg = 12;
                    dc = 10;
                    duration = 30f;
                    break;
                case 2:
                    dmg = 21;
                    dc = 15;
                    duration = 60f;
                    break;
                case 3:
                    dmg = 34;
                    dc = 20;
                    duration = 60f;
                    break;
            }

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.Ranged);

            var attackerStat = CombatService.GetPerkAdjustedAbilityScore(activator);
            var attack = StatService.GetAttack(activator, AbilityType.Might, SkillType.Ranged);
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

            dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Reflex, dc);
            var checkResult = ReflexSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                StatusEffectService.Apply(activator, target, StatusEffectType.Bleed, duration);
            }

            CombatPointService.AddCombatPoint(activator, target, SkillType.Ranged, 3);
            EnmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void PiercingToss1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.PiercingToss1, PerkType.PiercingToss)
                .Name("Piercing Toss I")
                .Level(1)
                .HasRecastDelay(RecastGroup.PiercingToss, 60f)
                .HasActivationDelay(0.5f)
                .HasMaxRange(15.0f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .UsesAnimation(AnimationType.ThrowGrenade)
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void PiercingToss2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.PiercingToss2, PerkType.PiercingToss)
                .Name("Piercing Toss II")
                .Level(2)
                .HasRecastDelay(RecastGroup.PiercingToss, 60f)
                .HasActivationDelay(0.5f)
                .HasMaxRange(15.0f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .UsesAnimation(AnimationType.ThrowGrenade)
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void PiercingToss3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.PiercingToss3, PerkType.PiercingToss)
                .Name("Piercing Toss III")
                .Level(3)
                .HasRecastDelay(RecastGroup.PiercingToss, 60f)
                .HasActivationDelay(0.5f)
                .HasMaxRange(15.0f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .UsesAnimation(AnimationType.ThrowGrenade)
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
