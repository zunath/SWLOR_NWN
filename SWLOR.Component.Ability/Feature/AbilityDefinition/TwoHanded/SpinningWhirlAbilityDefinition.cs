//using Random = SWLOR.Game.Server.Service.Random;

using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Constants;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.TwoHanded
{
    public class SpinningWhirlAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public SpinningWhirlAbilityDefinition(IServiceProvider serviceProvider)
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
            SpinningWhirl1(builder);
            SpinningWhirl2(builder);
            SpinningWhirl3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location targetLocation)
        {
            var weapon = GetItemInSlot(InventorySlotType.RightHand, activator);

            if (!ItemService.TwinBladeBaseItemTypes.Contains(GetBaseItemType(weapon)))
            {
                return "This is a twin blade ability.";
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
                    dmg = 10;
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

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.TwoHanded);

            var count = 0;
            var creature = GetFirstObjectInShape(ShapeType.Sphere, RadiusSize.Large, GetLocation(activator), true, ObjectType.Creature);
            while (GetIsObjectValid(creature) && count < 3)
            {
                if(GetIsReactionTypeHostile(creature, activator))
                {
                    var attackerStat = GetAbilityScore(activator, AbilityType.Might);
                    var attack = StatService.GetAttack(activator, AbilityType.Might, SkillType.TwoHanded);
                    var defense = StatService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
                    var defenderStat = GetAbilityScore(creature, AbilityType.Vitality);
                    var damage = CombatService.CalculateDamage(
                        attack,
                        dmg,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    var dTarget = creature;

                    DelayCommand(0.1f, () =>
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), dTarget));

                    CombatPointService.AddCombatPoint(activator, creature, SkillType.TwoHanded, 3);
                    EnmityService.ModifyEnmity(activator, creature, 100 * level + damage);
                    count++;
                }
                creature = GetNextObjectInShape(ShapeType.Sphere, RadiusSize.Large, GetLocation(activator), true, ObjectType.Creature);
            }

            AssignCommand(activator, () => ActionPlayAnimation(AnimationType.Whirlwind));
        }

        private void SpinningWhirl1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SpinningWhirl1, PerkType.SpinningWhirl)
                .Name("Spinning Whirl I")
                .Level(1)
                .HasRecastDelay(RecastGroup.SpinningWhirl, 30f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void SpinningWhirl2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SpinningWhirl2, PerkType.SpinningWhirl)
                .Name("Spinning Whirl II")
                .Level(2)
                .HasRecastDelay(RecastGroup.SpinningWhirl, 30f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void SpinningWhirl3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SpinningWhirl3, PerkType.SpinningWhirl)
                .Name("Spinning Whirl III")
                .Level(3)
                .HasRecastDelay(RecastGroup.SpinningWhirl, 30f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
