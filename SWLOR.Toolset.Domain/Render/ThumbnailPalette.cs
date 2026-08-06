namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// The tone a thumbnail falls back to for meshes with no resolvable texture, and the background the
    /// rest is drawn over.
    /// </summary>
    /// <remarks>
    /// Deliberately a cool interface grey rather than anything trying to look like material: it has to sit
    /// beside textured meshes in the same render without reading as a different object, which a saturated
    /// stand-in colour would.
    /// </remarks>
    public sealed record ThumbnailPalette(uint Background, byte BaseR, byte BaseG, byte BaseB, float Ambient)
    {
        /// <summary>Matches the palette tile's own field background and accent-leaning material.</summary>
        public static readonly ThumbnailPalette Default =
            new(Background: 0x00000000, BaseR: 0x9E, BaseG: 0xB6, BaseB: 0xD8, Ambient: 0.35f);
    }
}
