//using Random = SWLOR.Game.Server.Service.Random;

using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.MartialArts
{
    public class StrikingCobraAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IStatCalculationService _statCalculation;

        public StrikingCobraAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
        {
            _serviceProvider = serviceProvider;
            _statCalculation = statCalculation;
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();

        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            StrikingCobra1(builder);
            StrikingCobra2(builder);
            StrikingCobra3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlotType.RightHand, activator);

            if (!ItemService.KatarBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a katar ability.";
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
                    dmg = 6;
                    dc = 10;
                    duration = 30f;
                    break;
                case 2:
                    dmg = 15;
                    dc = 15;
                    duration = 60f;
                    break;
                case 3:
                    dmg = 22;
                    dc = 20;
                    duration = 60f;
                    break;
            }

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.MartialArts);

            CombatPointService.AddCombatPoint(activator, target, SkillType.MartialArts, 3);

            var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
            var attack = _statCalculation.CalculateAttack(activator, AbilityType.Might, SkillType.MartialArts);
            var defense = _statCalculation.CalculateDefense(target);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = CombatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Bludgeoning), target);

            dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Will, dc);
            var checkResult = ReflexSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
            }

            EnmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void StrikingCobra1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.StrikingCobra1, PerkType.StrikingCobra)
                .Name("Striking Cobra I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.StrikingCobra, 60f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void StrikingCobra2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.StrikingCobra2, PerkType.StrikingCobra)
                .Name("Striking Cobra II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.StrikingCobra, 60f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void StrikingCobra3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.StrikingCobra3, PerkType.StrikingCobra)
                .Name("Striking Cobra III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.StrikingCobra, 60f)
                .RequirementStamina(8)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}

