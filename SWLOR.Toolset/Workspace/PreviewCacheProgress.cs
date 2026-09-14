namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// How far a preview-cache build has got, reported to the Output pane as it runs.
    /// </summary>
    /// <param name="Processed">Blueprints examined so far.</param>
    /// <param name="Total">Blueprints in the build.</param>
    /// <param name="Rendered">Previews newly rendered and written to disk.</param>
    /// <param name="Reused">Blueprints skipped because a current cache entry already existed.</param>
    /// <param name="WithoutArtwork">Blueprints that have no artwork and will show a type symbol.</param>
    /// <param name="Failed">
    /// Blueprints whose render failed outright. Not cached either way, so the next build retries them -
    /// which is why this is reported separately from <paramref name="WithoutArtwork"/> rather than folded
    /// into it.
    /// </param>
    public sealed record PreviewCacheProgress(
        int Processed,
        int Total,
        int Rendered,
        int Reused,
        int WithoutArtwork,
        int Failed = 0)
    {
        public int PercentComplete => Total <= 0 ? 100 : (int)(Processed * 100L / Total);
    }
}
