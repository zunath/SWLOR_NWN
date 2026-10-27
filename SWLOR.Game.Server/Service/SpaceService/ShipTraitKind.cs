namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>Where a component trait comes from (plan section 14).</summary>
    public enum ShipTraitKind
    {
        Invalid = 0,

        /// <summary>A crafted variant: a trade-off chosen by the recipe.</summary>
        Crafted = 1,

        /// <summary>A small bonus a salvaged component carries.</summary>
        Common = 2,

        /// <summary>A salvage-only trait crafting cannot make.</summary>
        Rare = 3,
    }
}
