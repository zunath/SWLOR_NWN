using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.OneHanded
{
    public class ForceLeapAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public ForceLeapAbilityDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private IAbilityService AbilityService => _serviceProvider.GetRequiredService<IAbilityService>();
        private ICombatService CombatService => _serviceProvider.GetRequiredService<ICombatService>();
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICombatPointService CombatPointService => _serviceProvider.GetRequiredService<ICombatPointService>();
        private IEnmityService EnmityService => _serviceProvider.GetRequiredService<IEnmityService>();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceLeap1(builder);
            ForceLeap2(builder);
            ForceLeap3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlotType.RightHand, activator);
            var rightHandType = GetBaseItemType(weapon);

            if (!ItemService.LightsaberBaseItemTypes.Contains(rightHandType))
            {
                return "A lightsaber must be equipped in your right hand to use this ability.";
            }

            if (GetDistanceBetween(activator, target) < 8)
            {
                return "You must get further away from the target to use this ability.";
            }

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
                    dmg = 15;
                    break;
                case 3:
                    dmg = 23;
                    break;
                default:
                    break;
            }

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.OneHanded);

            const float Delay = 1.2f;
            ClearAllActions();
            AssignCommand(activator, () =>
            {
                PlaySound("plr_force_flip");
                ActionPlayAnimation(AnimationType.ForceLeap, 2.0f, 1.0f);
                SetCommandable(false, activator);
            });
            
            CombatPointService.AddCombatPoint(activator, target, SkillType.OneHanded, 3);

            var stat = AbilityType.Perception;
            if (AbilityService.IsAbilityToggled(activator, AbilityToggleType.StrongStyleLightsaber))
            {
                stat = AbilityType.Might;
            }

            var attackerStat = CombatService.GetPerkAdjustedAbilityScore(activator);
            var attack = StatService.GetAttack(activator, stat, SkillType.OneHanded);
            var defense = StatService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
            var damage = CombatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);
            var weapon = GetItemInSlot(InventorySlotType.RightHand, activator);
            var rightHandBaseItemType = GetBaseItemType(weapon);
            
            DelayCommand(Delay, () =>
            {
                const float Duration = 2f;
                SetCommandable(true, activator);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
                ApplyEffectToObject(DurationType.Temporary, EffectStunned(), target, Duration);
                AbilityService.ApplyTemporaryImmunity(target, Duration, ImmunityType.Stun);
                AssignCommand(activator, () =>
                {
                    if (ItemService.LightsaberBaseItemTypes.Contains(rightHandBaseItemType))
                    {
                        PlaySound("cb_ht_saberchan1");
                    }
                    else
                    {
                        PlaySound("cb_ht_critical");
                    }
                    ActionJumpToObject(target);
                });
            });
            EnmityService.ModifyEnmity(activator, target, 250 * level + damage);
        }

        private void ForceLeap1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLeap1, PerkType.ForceLeap)
                .Name("Force Leap I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ForceLeap, 30f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(3)
                .HasMaxRange(20f)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void ForceLeap2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLeap2, PerkType.ForceLeap)
                .Name("Force Leap II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ForceLeap, 30f)
                .RequirementStamina(4)
                .HasActivationDelay(0.5f)
                .HasMaxRange(20f)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void ForceLeap3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLeap3, PerkType.ForceLeap)
                .Name("Force Leap III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.ForceLeap, 30f)
                .RequirementStamina(5)
                .HasActivationDelay(0.5f)
                .HasMaxRange(20f)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
