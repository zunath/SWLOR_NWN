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

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.OneHanded
{
    public class RiotBladeAbilityDefinition : IAbilityListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public RiotBladeAbilityDefinition(IServiceProvider serviceProvider)
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
            RiotBlade1(builder);
            RiotBlade2(builder);
            RiotBlade3(builder);

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
            var dmg = 0;



            switch (level)
            {
                case 1:
                    dmg = 10;
                    break;
                case 2:
                    dmg = 20;
                    break;
                case 3:
                    dmg = 30;
                    break;
                default:
                    break;
            }

            dmg += CombatService.GetAbilityDamageBonus(activator, SkillType.OneHanded);

            CombatPointService.AddCombatPoint(activator, target, SkillType.OneHanded, 3);

            var might = GetAbilityScore(activator, AbilityType.Might);
            var attack = StatService.GetAttack(activator, AbilityType.Might, SkillType.OneHanded);
            var defense = StatService.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var vitality = GetAbilityModifier(AbilityType.Vitality, target);
            var damage = CombatService.CalculateDamage(attack, dmg, might, defense, vitality, 0);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);

            AssignCommand(activator, () => ActionPlayAnimation(AnimationType.RiotBlade));

            EnmityService.ModifyEnmity(activator, target, 100 * level + damage);
        }

        private void RiotBlade1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.RiotBlade1, PerkType.RiotBlade)
                .Name("Riot Blade I")
                .Level(1)
                .HasRecastDelay(RecastGroup.RiotBlade, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void RiotBlade2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.RiotBlade2, PerkType.RiotBlade)
                .Name("Riot Blade II")
                .Level(2)
                .HasRecastDelay(RecastGroup.RiotBlade, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
        private void RiotBlade3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.RiotBlade3, PerkType.RiotBlade)
                .Name("Riot Blade III")
                .Level(3)
                .HasRecastDelay(RecastGroup.RiotBlade, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasCustomValidation(Validation)
                .HasImpactAction(ImpactAction);
        }
    }
}
