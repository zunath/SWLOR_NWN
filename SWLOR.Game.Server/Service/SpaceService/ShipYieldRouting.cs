using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Storage-first routing of mining, gas and salvage yields (plan section 12): a yield fills the ship's hold while it
    /// has room and reaches the pilot's inventory only when the hold is full or the ship has none.
    /// </summary>
    public static class ShipYieldRouting
    {
        /// <summary>
        /// Plans one item's yield: it tops up the hold's stacks of the same item first (up to
        /// <paramref name="maxStack"/>), then opens new stacks while the hold has free entries, and the rest overflows.
        /// </summary>
        /// <param name="existingQuantities">The quantities of the hold's stacks of this item.</param>
        /// <param name="maxStack">The item's largest stack (1 for items that do not stack).</param>
        /// <param name="freeEntries">Storage entries left in the ship (the ship's limit minus every stored stack).</param>
        /// <param name="quantity">How many of the item the cycle yielded.</param>
        public static ShipYieldPlan Plan(IReadOnlyList<int> existingQuantities, int maxStack, int freeEntries, int quantity)
        {
            var plan = new ShipYieldPlan();
            var remaining = Math.Max(0, quantity);
            var stackLimit = Math.Max(1, maxStack);

            for (var index = 0; index < existingQuantities.Count && remaining > 0; index++)
            {
                var room = stackLimit - existingQuantities[index];
                if (room <= 0)
                    continue;

                var added = Math.Min(room, remaining);
                plan.TopUps.Add((index, existingQuantities[index] + added));
                remaining -= added;
            }

            var entries = Math.Max(0, freeEntries);
            while (remaining > 0 && entries > 0)
            {
                var added = Math.Min(stackLimit, remaining);
                plan.NewStacks.Add(added);
                remaining -= added;
                entries--;
            }

            plan.Overflow = remaining;
            return plan;
        }

        /// <summary>
        /// Whether the pilot may put yields in the ship's hold: the ship's owner always may, anyone else only with
        /// storage access to the hold, so a borrowed ship never takes a pilot's yields where they cannot reach them.
        /// </summary>
        public static bool CanUseHold(bool isOwner, bool hasHoldAccess) => isOwner || hasHoldAccess;
    }
}
