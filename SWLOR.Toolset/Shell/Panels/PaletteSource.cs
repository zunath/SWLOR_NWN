namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// Which half of the palette is showing: the base game's own content or the module's.
    /// </summary>
    /// <remarks>
    /// The split Aurora had, for the reason Aurora had it - "is this thing mine or does it ship with the
    /// game" decides whether you may rename it, delete it, or rely on it being there on a fresh install.
    /// Custom is the default because it is where a builder spends essentially all of their time.
    /// </remarks>
    public enum PaletteSource
    {
        /// <summary>Blueprints that exist as files in this module.</summary>
        Custom,

        /// <summary>Blueprints the base game ships, read from its own palettes. Read-only.</summary>
        Standard
    }
}
