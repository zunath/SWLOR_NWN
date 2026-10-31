using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Where one item's yield goes (<see cref="ShipYieldRouting.Plan"/>): onto existing hold stacks, into new hold
    /// stacks, and the rest to the pilot.
    /// </summary>
    public class ShipYieldPlan
    {
        /// <summary>Existing hold stacks that grow: the stack's index in the list given to the plan, and its new quantity.</summary>
        public List<(int Index, int NewQuantity)> TopUps { get; } = new();

        /// <summary>The quantity of each new hold stack. Each takes one storage entry.</summary>
        public List<int> NewStacks { get; } = new();

        /// <summary>What the hold has no room for: it goes to the pilot's inventory.</summary>
        public int Overflow { get; set; }

        /// <summary>How many of the items the hold takes.</summary>
        public int Stored(IReadOnlyList<int> existingQuantities) =>
            TopUps.Sum(x => x.NewQuantity - existingQuantities[x.Index]) + NewStacks.Sum();
    }
}
