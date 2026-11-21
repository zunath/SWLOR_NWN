using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Service.SlicingService
{
    /// <summary>One part of a space slicing success: credits, or an item (a component carries its trait).</summary>
    public sealed class SpaceSlicingReward
    {
        public SpaceSlicingRewardKind Kind { get; init; }

        /// <summary>The kind the table picked, before an empty pool fell back to <see cref="Kind"/>.</summary>
        public SpaceSlicingRewardKind PickedKind { get; init; }

        /// <summary>The item resref, or null for credits.</summary>
        public string Resref { get; init; }

        public int Quantity { get; init; } = 1;
        public ShipComponentTraitType Trait { get; init; }
        public int Credits { get; init; }

        public bool IsCredits => Kind == SpaceSlicingRewardKind.Credits;
    }
}
