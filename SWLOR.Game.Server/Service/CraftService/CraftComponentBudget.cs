using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService
{
    public sealed record CraftComponentStack(uint Item, string Resref, int Quantity);
    public sealed record CraftComponentReservation(uint Item, int Quantity);

    public static class CraftComponentBudget
    {
        // Return a complete reservation plan before the caller mutates inventory.
        // An empty plan reserves nothing, including when only one material is missing.
        public static IReadOnlyList<CraftComponentReservation> Plan(
            IReadOnlyDictionary<string, int> required, IReadOnlyList<CraftComponentStack> inventory)
        {
            var remaining = required.ToDictionary(entry => entry.Key, entry => entry.Value);
            var plan = new List<CraftComponentReservation>();
            for (var index = inventory.Count - 1; index >= 0; index--)
            {
                var stack = inventory[index];
                if (!remaining.TryGetValue(stack.Resref, out var needed)) continue;
                var quantity = Math.Min(stack.Quantity, needed);
                if (quantity <= 0) continue;
                plan.Add(new CraftComponentReservation(stack.Item, quantity));
                remaining[stack.Resref] -= quantity;
                if (remaining[stack.Resref] == 0) remaining.Remove(stack.Resref);
            }
            return remaining.Count == 0 ? plan.AsReadOnly() : Array.Empty<CraftComponentReservation>();
        }
    }
}
