using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// Adds an inventory item requirement to activate a perk.
    /// </summary>
    public class AbilityRequirementItem : IAbilityActivationRequirement
    {
        public string ItemResref { get; }
        public int Quantity { get; }
        public StatType PreserveChanceStatType { get; }

        public AbilityRequirementItem(
            string itemResref,
            int quantity = 1,
            StatType preserveChanceStatType = StatType.Invalid)
        {
            ItemResref = itemResref;
            Quantity = Math.Max(1, quantity);
            PreserveChanceStatType = preserveChanceStatType;
        }

        public string CheckRequirements(uint player, AbilityDetail ability = null)
        {
            if (GetIsDM(player) || !GetIsPC(player))
                return string.Empty;

            var total = 0;
            foreach (var item in GetMatchingItems(player))
            {
                total += GetItemStackSize(item);
                if (total >= Quantity)
                    return string.Empty;
            }

            var itemName = Cache.GetItemNameByResref(ItemResref);
            return Quantity == 1
                ? $"Requires {itemName}."
                : $"Requires {Quantity} {itemName}.";
        }

        public void AfterActivationAction(uint player, AbilityDetail ability = null)
        {
            if (GetIsDM(player) || !GetIsPC(player))
                return;

            if (ShouldPreserveItem(player))
                return;

            // Supplies may be split across several stacks and bags, so drain stacks until the cost is paid.
            var remaining = Quantity;
            foreach (var item in GetMatchingItems(player))
            {
                var amount = Math.Min(remaining, GetItemStackSize(item));
                if (amount > 0 && Item.ReduceItemStack(item, amount))
                    remaining -= amount;

                if (remaining <= 0)
                    return;
            }
        }

        /// <summary>
        /// Every stack with the required tag in the player's inventory, including stacks inside bags.
        /// Each inventory is read in full before any bag is opened, so nested iteration never resets
        /// an iterator that is still in use.
        /// </summary>
        private List<uint> GetMatchingItems(uint player)
        {
            var matches = new List<uint>();
            var seen = new HashSet<uint>();
            var inventories = new Queue<uint>();
            inventories.Enqueue(player);

            while (inventories.Count > 0)
            {
                var owner = inventories.Dequeue();
                var contents = new List<uint>();
                for (var item = GetFirstItemInInventory(owner); GetIsObjectValid(item); item = GetNextItemInInventory(owner))
                    contents.Add(item);

                foreach (var item in contents)
                {
                    if (!seen.Add(item))
                        continue;

                    if (GetTag(item) == ItemResref)
                        matches.Add(item);

                    if (GetHasInventory(item))
                        inventories.Enqueue(item);
                }
            }

            return matches;
        }

        private bool ShouldPreserveItem(uint player)
        {
            if (PreserveChanceStatType == StatType.Invalid)
                return false;

            var chance = Stat.GetStatAdjustment(player, PreserveChanceStatType);
            return chance > 0 && Random.D100(1) <= chance;
        }
    }
}
