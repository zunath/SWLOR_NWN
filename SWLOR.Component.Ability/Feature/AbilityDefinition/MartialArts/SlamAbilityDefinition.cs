//using Random = SWLOR.Game.Server.Service.Random;

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

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.MartialArts
{
    public class SlamAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public SlamAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Slam1(builder);
            Slam2(builder);
            Slam3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlotType.RightHand, activator);

            if (!ItemService.StaffBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a staff ability.";
            }
            else
                return string.Empty;
        }

        private void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {

            int dmg;
            int dc;
            const float Duration = 12f;

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

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.MartialArts);

            EnmityService.ModifyEnmityOnAll(activator, 100 * level);
            CombatPointService.AddCombatPoint(activator, target, SkillType.MartialArts, 3);

            var attackerStat = CombatService.GetPerkAdjustedAbilityScore(activator);
            int attack;

            if (GetHasFeat(FeatType.FlurryStyle, activator))
            {
                attack = StatService.GetAttack(activator, AbilityType.Perception, SkillType.MartialArts);
            }
            else
            {
                attack = StatService.GetAttack(activator, AbilityType.Might, SkillType.MartialArts);
            }

            var defense = StatService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = CombatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Bludgeoning), target);

            dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Fortitude, dc);
            var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);

            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectBlindness(), target, Duration);
            }
        }

        private void Slam1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Slam1, PerkType.Slam)
                .Name("Slam I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.Slam, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void Slam2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Slam2, PerkType.Slam)
                .Name("Slam II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.Slam, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void Slam3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Slam3, PerkType.Slam)
                .Name("Slam III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.Slam, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
