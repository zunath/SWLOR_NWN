using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using AssociateType = SWLOR.Game.Server.Core.NWScript.Enum.Associate.AssociateType;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public class RewardAbilityDefinition : IAbilityListDefinition
    {
        private const string PetTreatTag = "pet_treat";

        private readonly AbilityBuilder _builder = new();


        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Reward1();
            Reward2();
            Reward3();

            return _builder.Build();
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
            Item.ReduceItemStack(item, 1);
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
            if (!BeastMastery.IsPlayerBeast(beast))
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
            var amount = baseHealingAmount + willBonus * 8 + Random.D10(1);

            var beast = GetAssociate(AssociateType.Henchman, activator);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), beast);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), beast);

            TakePetTreat(activator);
            Enmity.ModifyEnmityOnAll(activator, 300 + amount);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
        }

        private void Reward1()
        {
            _builder.Create(FeatType.Reward1, PerkType.Reward)
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

        private void Reward2()
        {
            _builder.Create(FeatType.Reward2, PerkType.Reward)
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

        private void Reward3()
        {
            _builder.Create(FeatType.Reward3, PerkType.Reward)
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