namespace SWLOR.Toolset.Editors
{
    /// <summary>Which edit a paint dab applies to the tile under the cursor (WP7.3).</summary>
    public enum AreaPaintTool
    {
        /// <summary>Fill the tile with the selected terrain and blend its neighbours.</summary>
        Terrain,

        /// <summary>Turn the tile a quarter turn counter-clockwise.</summary>
        Rotate,

        /// <summary>Raise the tile one height step.</summary>
        Raise,

        /// <summary>Lower the tile one height step (never below zero).</summary>
        Lower
    }
}
