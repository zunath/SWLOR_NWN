namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>A slicing tool in the player's inventory that can still be used on the attempt.</summary>
    public sealed class EligibleSlicingTool
    {
        public uint Item { get; init; }
        public string Name { get; init; }
        public SlicingToolType Type { get; init; }
        public int Tier { get; init; }
    }
}
