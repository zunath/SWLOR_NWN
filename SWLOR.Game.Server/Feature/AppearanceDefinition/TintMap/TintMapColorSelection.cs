namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    /// <summary>
    /// The resolved value applied to one layer: either an RGB override, or a palette row retained
    /// for standard NWN colors and old tint-map locals.
    /// </summary>
    public readonly record struct TintMapColorSelection(int PaletteColorId, TintMapColor? CustomColor);
}
