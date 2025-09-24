using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Common.Enums;
using AssociateType = SWLOR.NWN.API.NWScript.Enum.Associate.AssociateType;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beastmaster
{
    public class RewardAbilityDefinition : IAbilityListDefinition
    {
        private readonly IRandomService _random;
        private const string PetTreatTag = "pet_treat";

        private readonly IItemService _itemService;
        private readonly ICombatPointService _combatPointService;
        private readonly IBeastMasteryService _beastMastery;
        private readonly IEnmityService _enmityService;

        public RewardAbilityDefinition(
            IRandomService random, 
            IItemService itemService, 
            ICombatPointService combatPointService, 
            IBeastMasteryService beastMastery, 
            IEnmityService enmityService)
        {
            _random = random;
            _itemService = itemService;
            _combatPointService = combatPointService;
            _beastMastery = beastMastery;
            _enmityService = enmityService;
        }


        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Reward1(builder);
            Reward2(builder);
            Reward3(builder);

            return builder.Build();
        }
        
        private bool HasPetTreat(uint activator)
        {
            // NPCs don't need supplies.
            if (!GetIsPC(activator))
                return true;

            var item = GetItemPossessedBy(activator, PetTreatTag);

            return GetIsObjectValid(item) && GetItemStackSize(item) > 0;
        }

        private void TakePetTreat(uint activator)
        {
            // NPCs don't need supplies.
            if (!GetIsPC(activator))
                return;
            
            var item = GetItemPossessedBy(activator, PetTreatTag);
            _itemService.ReduceItemStack(item, 1);
        }


        private string Validation(uint activator)
        {
            if (!GetIsPC(activator) || GetIsDM(activator) || GetIsDMPossessed(activator))
            {
                return "Only players may use this ability.";
            }

            if (!HasPetTreat(activator))
            {
                return "You have no pet treats.";
            }

            var beast = GetAssociate(AssociateType.Henchman, activator);
            if (!_beastMastery.IsPlayerBeast(beast))
            {
                return "You do not have an active beast.";
            }

            if (GetDistanceBetween(beast, activator) >= 15f)
            {
                return "Your beast is too far away.";
            }

            return string.Empty;
        }

        private void Impact(uint activator, int baseHealingAmount)
        {
            var willBonus = GetAbilityModifier(AbilityType.Social, activator);
            var beast = GetAssociate(AssociateType.Henchman, activator);
            var maxHP = GetMaxHitPoints(beast);
            var amount = baseHealingAmount + willBonus * 10 + (maxHP / 5) + _random.D10(1);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), beast);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), beast);

            TakePetTreat(activator);
            _enmityService.ModifyEnmityOnAll(activator, 300 + amount);
            _combatPointService.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
        }

        private void Reward1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Reward1, PerkType.Reward)
                .Name("Reward I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Reward, 18f)
                .UsesAnimation(Animation.LoopingGetMid)
                .RequirementStamina(6)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) => Validation(activator))
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    Impact(activator, 50);
                });
        }

        private void Reward2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Reward2, PerkType.Reward)
                .Name("Reward II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Reward, 18f)
                .UsesAnimation(Animation.LoopingGetMid)
                .RequirementStamina(8)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) => Validation(activator))
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    Impact(activator, 90);
                });
        }

        private void Reward3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Reward3, PerkType.Reward)
                .Name("Reward III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Reward, 18f)
                .UsesAnimation(Animation.LoopingGetMid)
                .RequirementStamina(10)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) => Validation(activator))
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    Impact(activator, 130);
                });
        }
    }
}
