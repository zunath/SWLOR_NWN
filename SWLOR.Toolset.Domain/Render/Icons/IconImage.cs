namespace SWLOR.Toolset.Domain.Render.Icons
{
    /// <summary>
    /// A finished tile image: straight (unpremultiplied) BGRA, top-down, ready to hand to a
    /// <c>WriteableBitmap</c>.
    /// </summary>
    /// <remarks>
    /// Carries its own dimensions rather than assuming a square, because the sources do not agree on
    /// one: model thumbnails are rendered square, while an inventory icon is shaped like the inventory
    /// slot it was drawn for (a rifle is 32x96). Keeping the native shape and letting the view fit it
    /// avoids resampling the artwork twice.
    /// </remarks>
    public sealed record IconImage(int Width, int Height, byte[] Bgra)
    {
        /// <summary>Bytes per pixel in <see cref="Bgra"/>.</summary>
        public const int BytesPerPixel = 4;

        /// <summary>Row stride in bytes.</summary>
        public int Stride => Width * BytesPerPixel;
    }
}
