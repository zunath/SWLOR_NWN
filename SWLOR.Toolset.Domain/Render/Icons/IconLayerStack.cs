namespace SWLOR.Toolset.Domain.Render.Icons
{
    /// <summary>
    /// One candidate icon: the texture resrefs to draw, bottom layer first. Most icons are a single
    /// layer; NWN's composite weapons are three (blade, hilt, pommel) stacked with alpha.
    /// </summary>
    /// <param name="Layers">Texture resrefs in painting order. Layers that do not resolve are skipped.</param>
    /// <param name="Status">A human-readable note for status/tooltip display.</param>
    public sealed record IconLayerStack(IReadOnlyList<string> Layers, string Status);
}
