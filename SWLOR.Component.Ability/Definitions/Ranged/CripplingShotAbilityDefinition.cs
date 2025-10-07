//using Random = SWLOR.Game.Server.Service.Random;

using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.Ranged
{
    public class CripplingShotAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public CripplingShotAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
        {
            _serviceProvider = serviceProvider;
            _statCalculation = statCalculation;
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            CripplingShot1(builder);
            CripplingShot2(builder);
            CripplingShot3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlotType.RightHand, activator);

            if (!ItemService.RifleBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a rifle ability.";
            }
            else
                return string.Empty;
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {


            int dmg;
            const float Duration = 6f;
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

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.Ranged);

            CombatPointService.AddCombatPoint(activator, target, SkillType.Ranged, 3);

            var attackerStat = CombatService.GetPerkAdjustedAbilityScore(activator);
            var attack = _statCalculation.CalculateAttack(activator, AbilityType.Perception, SkillType.Ranged);
            var defense = _statCalculation.CalculateDefense(target);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = CombatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);

            dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Reflex, dc);
            var checkResult = ReflexSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectMovementSpeedDecrease(99), target, Duration);
            }

            EnmityService.ModifyEnmity(activator, target, 250 * level + damage);
        }

        private void CripplingShot1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CripplingShot1, PerkType.CripplingShot)
                .Name("Crippling Shot I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.CripplingShot, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void CripplingShot2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CripplingShot2, PerkType.CripplingShot)
                .Name("Crippling Shot II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.CripplingShot, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void CripplingShot3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CripplingShot3, PerkType.CripplingShot)
                .Name("Crippling Shot III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.CripplingShot, 60f)
                .RequirementStamina(8)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
