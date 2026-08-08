namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Decides whether a decoded texture has to be cut out rather than drawn solid.
    /// </summary>
    /// <remarks>
    /// NWN signals transparency two ways, and only one of them is a TXI. Most textures that need it
    /// carry no TXI at all - the alpha channel simply is the signal - so a renderer that honours only
    /// the hint draws them opaque, and an opaque draw of a transparent texel shows whatever colour the
    /// compressed block happens to hold there. For DXT that is usually black.
    /// <para>
    /// A tileset floor grating is what exposed this: cz220shipbreakin lays 62 tiles of zsf01_d05_01,
    /// whose floor is a see-through grate (<c>zsf01_bridge</c>, 32% of its texels fully transparent)
    /// suspended 1.5m above a solid floor. Every one of those tiles drew as a solid black square
    /// instead of a grate you can see through.
    /// </para>
    /// <para>
    /// The result is a hard cutoff, not blending. Sorted alpha blending is a bigger change than this
    /// warrants, and for the grates, fences and foliage that need it a cutoff is what the artwork was
    /// drawn for anyway.
    /// </para>
    /// </remarks>
    public static class TextureAlphaPolicy
    {
        /// <summary>
        /// Fragments below half alpha are dropped - the midpoint, so a grate keeps its bars and loses
        /// its holes.
        /// </summary>
        public const float PunchThroughCutoff = 0.5f;

        /// <summary>
        /// How much of a texture must be transparent before it is treated as cut-out. A threshold
        /// rather than "any transparent texel at all", so a compression artefact at a block edge or a
        /// single stray pixel cannot start punching holes in a solid surface.
        /// </summary>
        public const float TransparentTexelShare = 0.01f;

        /// <summary>
        /// Whether enough of <paramref name="image"/> is transparent to warrant a cutoff. False for a
        /// null image, a degenerate one, or one whose pixel buffer is too short to describe it - a
        /// texture that decoded badly should draw wrongly, not throw mid-frame.
        /// </summary>
        public static bool RequiresCutoff(TextureImage? image)
        {
            if (image == null || image.Width <= 0 || image.Height <= 0)
                return false;

            var texels = image.Width * image.Height;
            if (image.Pixels.Length < texels * 4)
                return false;

            var transparent = 0;
            var threshold = (int)(texels * TransparentTexelShare);

            for (var i = 0; i < texels; i++)
            {
                if (image.Pixels[i * 4 + 3] >= 128)
                    continue;

                if (++transparent > threshold)
                    return true;
            }

            return false;
        }
    }
}
