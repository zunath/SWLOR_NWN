using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public abstract class FirstAidBaseAbilityDefinition: IAbilityListDefinition
    {
        protected readonly AbilityBuilder Builder = new AbilityBuilder();
        private const string MedicalSuppliesItemTag = "med_supplies";
        private const string StimPackItemTag = "stim_pack";

        public abstract Dictionary<FeatType, AbilityDetail> BuildAbilities();

        private static void TakeItem(uint activator, string resref)
        {
            if (!GetIsPC(activator))
                return;

            var chanceToNotConsume = 10 * Perk.GetEffectivePerkLevel(activator, PerkType.FrugalMedic);
            if (Random.D100(1) <= chanceToNotConsume)
                return;

            var item = GetItemPossessedBy(activator, resref);
            var stackSize = GetItemStackSize(item);

            if (stackSize <= 1)
            {
                DestroyObject(item);
            }
            else
            {
                SetItemStackSize(item, stackSize - 1);
            }
        }

        /// <summary>
        /// Determines whether the activator has at least one Medical Supplies item in their inventory.
        /// </summary>
        /// <param name="activator">The activator of the ability.</param>
        /// <returns>true if at least one medical supplies item is found, false otherwise.</returns>
        protected bool HasMedicalSupplies(uint activator)
        {
            // NPCs don't need supplies.
            if (!GetIsPC(activator))
                return true;

            var item = GetItemPossessedBy(activator, MedicalSuppliesItemTag);

            return GetIsObjectValid(item) && GetItemStackSize(item) > 0;
        }

        /// <summary>
        /// Determines whether the activator has at least one Stim Pack item in their inventory.
        /// </summary>
        /// <param name="activator">The activator of the ability.</param>
        /// <returns>true if at least one stim pack is found, false otherwise.</returns>
        protected bool HasStimPack(uint activator)
        {
            // NPCs don't need supplies.
            if (!GetIsPC(activator))
                return true;

            var item = GetItemPossessedBy(activator, StimPackItemTag);

            return GetIsObjectValid(item) && GetItemStackSize(item) > 0;
        }

        /// <summary>
        /// Takes medical supplies from the activator's inventory.
        /// If the activator is an NPC, no items will be taken.
        /// If activator has the Frugal Medic perk and passes the check, no item will be taken.
        /// </summary>
        /// <param name="activator">The activator to take supplies from.</param>
        protected void TakeMedicalSupplies(uint activator)
        {
            // NPCs don't need supplies.
            if (!GetIsPC(activator))
                return;

            TakeItem(activator, MedicalSuppliesItemTag);
        }

        /// <summary>
        /// Takes a stim pack from the activator's inventory.
        /// If the activator is an NPC, no items will be taken.
        /// If activator has the Frugal Medic perk and passes the check, no item will be taken.
        /// </summary>
        /// <param name="activator">The activator to take stim packs from.</param>
        protected void TakeStimPack(uint activator)
        {
            // NPCs don't need supplies.
            if (!GetIsPC(activator))
                return;

            TakeItem(activator, StimPackItemTag);
        }

        /// <summary>
        /// Return true if activator and target are within range, including the bonus provided by the
        /// Ranged Healing perk. Returns false otherwise.
        /// </summary>
        /// <param name="activator">The activator of the ability.</param>
        /// <param name="target">The target of the ability.</param>
        /// <returns>true if within range, false otherwise</returns>
        protected bool IsWithinRange(uint activator, uint target)
        {
            const float BaseDistance = 6f;
            var distance = BaseDistance + Perk.GetEffectivePerkLevel(activator, PerkType.RangedHealing);

            return !(GetDistanceBetween(activator, target) > distance);
        }
    }
}
