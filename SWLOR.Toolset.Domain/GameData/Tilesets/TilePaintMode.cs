namespace SWLOR.Toolset.Domain.GameData.Tilesets
{
    /// <summary>
    /// How the Tiles palette decides which tile a click lays down.
    /// </summary>
    public enum TilePaintMode
    {
        /// <summary>
        /// Pick a terrain and let the tileset's own rules choose the tile, re-blending the neighbours
        /// so the edges meet. This is how Aurora works, and it is what a builder wants nine times in
        /// ten: laying a floor is a statement about ground, not about which of forty corner pieces
        /// belongs at this particular junction.
        /// </summary>
        Auto,

        /// <summary>
        /// Pick an individual tile and stamp exactly that, at exactly the orientation chosen. The
        /// escape hatch for the case the rules cannot express - and the only way to place a tile the
        /// solver would never choose on its own.
        /// </summary>
        Manual
    }
}
