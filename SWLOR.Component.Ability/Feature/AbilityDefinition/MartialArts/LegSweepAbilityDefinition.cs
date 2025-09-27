using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
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
    public class LegSweepAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public LegSweepAbilityDefinition(IServiceProvider serviceProvider)
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
            LegSweep1(builder);
            LegSweep2(builder);
            LegSweep3(builder);

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
            const float Duration = 6f;

            switch (level)
            {
                default:
                case 1:
                    dmg = 4;
                    dc = 5;
                    break;
                case 2:
                    dmg = 13;
                    dc = 8;
                    break;
                case 3:
                    dmg = 20;
                    dc = 10;
                    break;
            }

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.MartialArts);

            EnmityService.ModifyEnmityOnAll(activator, 250 * level);
            CombatPointService.AddCombatPoint(activator, target, SkillType.MartialArts, 3);

            var attackerStat = CombatService.GetPerkAdjustedAbilityScore(activator);
            int attack;

            if(GetHasFeat(FeatType.FlurryStyle, activator))
            {
                attack = StatService.GetAttack(activator, AbilityType.Perception, SkillType.MartialArts);
            } 
            else
            {
                attack = StatService.GetAttack(activator, AbilityType.Might, SkillType.MartialArts);
            }
            var defense = StatService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = CombatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Bludgeoning), target);

            dc = CombatService.CalculateSavingThrowDC(activator, SavingThrowCategoryType.Reflex, dc);
            var checkResult = ReflexSave(target, dc, SavingThrowType.None, activator);
            if (checkResult == SavingThrowResultType.Failed)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, Duration);
                AbilityService.ApplyTemporaryImmunity(target, Duration, ImmunityType.Knockdown);
            }
        }

        private void LegSweep1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.LegSweep1, PerkType.LegSweep)
                .Name("Leg Sweep I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.LegSweep, 30f)
                .RequirementStamina(3)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void LegSweep2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.LegSweep2, PerkType.LegSweep)
                .Name("Leg Sweep II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.LegSweep, 30f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void LegSweep3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.LegSweep3, PerkType.LegSweep)
                .Name("Leg Sweep III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.LegSweep, 30f)
                .RequirementStamina(5)
                .IsWeaponAbility()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
