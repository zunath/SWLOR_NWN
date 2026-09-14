namespace SWLOR.Toolset.Domain.Render.Icons
{
    /// <summary>
    /// The three tones a type symbol is drawn in, as packed 0xAARRGGBB.
    /// </summary>
    /// <remarks>
    /// The defaults are the shell theme's own surface and ink values, so a symbol sits in the tile at the
    /// same weight as the panel around it: present, clearly a placeholder, and not competing with the
    /// real model renders beside it.
    /// </remarks>
    /// <param name="Fill">Body tone for solid shapes.</param>
    /// <param name="Stroke">Outlines, highlights, and the lit face of a solid.</param>
    /// <param name="Detail">Small marks - a doorknob, a coin edge, a dashed boundary.</param>
    public sealed record TypeIconPalette(uint Fill, uint Stroke, uint Detail)
    {
        public static readonly TypeIconPalette Default = new(0xFF39424F, 0xFF8A94A3, 0xFF636D7C);

        /// <summary>The same colour at <paramref name="factor"/> brightness, alpha untouched.</summary>
        public static uint Shade(uint color, float factor)
        {
            var alpha = color & 0xFF000000;
            var r = (byte)Math.Clamp(MathF.Round(((color >> 16) & 0xFF) * factor), 0, 255);
            var g = (byte)Math.Clamp(MathF.Round(((color >> 8) & 0xFF) * factor), 0, 255);
            var b = (byte)Math.Clamp(MathF.Round((color & 0xFF) * factor), 0, 255);
            return alpha | ((uint)r << 16) | ((uint)g << 8) | b;
        }
    }
}
