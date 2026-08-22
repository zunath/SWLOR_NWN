namespace SWLOR.Toolset.Domain.Render.Icons
{
    /// <summary>
    /// How much of a type symbol's detail is worth drawing at the size being asked for.
    /// </summary>
    /// <remarks>
    /// The same symbols serve two very different sizes: a palette tile is around 128px, while the type
    /// selector's buttons are around 18-22px. Detail that reads well on a tile - a dashed boundary, three
    /// stacked coins, two speaker waves - is finer than a pixel at button size, so it either disappears
    /// into the anti-aliasing or turns into a grey smudge. Rather than a second set of shapes, each symbol
    /// knows which of its parts are the recognisable ones and drops the rest.
    /// </remarks>
    public enum TypeIconDetail
    {
        /// <summary>Everything: the symbol as drawn on a palette tile.</summary>
        Full,

        /// <summary>Silhouette and one identifying mark, drawn with heavier strokes and less margin.</summary>
        Compact
    }
}
