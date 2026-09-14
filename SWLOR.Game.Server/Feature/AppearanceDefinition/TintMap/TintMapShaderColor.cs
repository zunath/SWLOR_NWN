namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    /// <summary>
    /// Lossless RGB transport through the existing scalar material parameter. Palette UVs
    /// occupy [0, 1); [1, 4) has exactly 2^24 distinct IEEE single-precision values.
    /// Using both exponent intervals preserves every RGB byte, including black and white,
    /// without changing MTR arity or adding parameters to composed client materials.
    /// Keep the inverse in the three fs_plt_* shaders in sync.
    /// </summary>
    public static class TintMapShaderColor
    {
        public static float Encode(TintMapColor color)
        {
            var packed = (color.Red << 16) | (color.Green << 8) | color.Blue;
            return packed < 8388608
                ? 1f + packed / 8388608f
                : 2f + (packed - 8388608) / 4194304f;
        }
    }
}
