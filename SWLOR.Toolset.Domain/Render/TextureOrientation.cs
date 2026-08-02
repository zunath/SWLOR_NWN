namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Reconciles the two vertical conventions in play when an NWN texture is drawn.
    /// </summary>
    /// <remarks>
    /// The decoders (<see cref="TextureLoader"/>) hand back rows top-down: pixel row 0 is the top of
    /// the image. NWN's models author their UVs bottom-up, so v = 0 means the <em>last</em> decoded
    /// row. A CPU rasterizer can reconcile the two while sampling - <c>ThumbnailRenderer.TrySample</c>
    /// indexes with <c>1 - v</c> - but a GPU upload has to hand the rows over already reversed,
    /// because <c>glTexImage2D</c> places the first row at v = 0.
    /// <para>
    /// Getting this wrong is quiet on tiling wall and floor artwork, which is why it survived: the
    /// texture still tiles, just upside down. It is loud on an atlas. The toolset marker models all
    /// sample <c>toolcolors</c>, a 16x16 sheet of four flat quadrants (yellow and blue over red and
    /// green), so half a texture of error silently swaps one colour for another - which is how a red
    /// waypoint came to draw as a yellow flag on a red arrow instead of a red flag on a yellow one.
    /// </para>
    /// </remarks>
    public static class TextureOrientation
    {
        /// <summary>
        /// Returns a copy of a tightly packed RGBA image with its rows reversed, converting between
        /// top-down decoded order and the bottom-up order a GL texture upload expects.
        /// </summary>
        /// <remarks>
        /// Returns <paramref name="rgba"/> itself when the dimensions do not describe it, so a
        /// malformed texture degrades to drawing wrongly rather than throwing mid-frame.
        /// </remarks>
        public static byte[] FlipRows(int width, int height, byte[] rgba)
        {
            ArgumentNullException.ThrowIfNull(rgba);

            var stride = width * 4;
            if (width <= 0 || height <= 0 || rgba.Length < stride * height)
                return rgba;

            var flipped = new byte[stride * height];
            for (var row = 0; row < height; row++)
                Array.Copy(rgba, row * stride, flipped, (height - 1 - row) * stride, stride);

            return flipped;
        }
    }
}
