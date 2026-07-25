namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// The colours a thumbnail is drawn with. Untextured, because a flat-shaded solid in the
    /// interface's own palette reads as a catalogue entry, while a half-lit textured render at 128px
    /// mostly reads as noise.
    /// </summary>
    public sealed record ThumbnailPalette(uint Background, byte BaseR, byte BaseG, byte BaseB, float Ambient)
    {
        /// <summary>Matches the palette tile's own field background and accent-leaning material.</summary>
        public static readonly ThumbnailPalette Default =
            new(Background: 0x00000000, BaseR: 0x9E, BaseG: 0xB6, BaseB: 0xD8, Ambient: 0.35f);
    }
}
