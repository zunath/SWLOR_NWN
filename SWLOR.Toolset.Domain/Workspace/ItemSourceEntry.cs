namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// One place a player can obtain a specific item resref, as found by
    /// <see cref="ItemObtainabilityIndex"/>.
    /// </summary>
    /// <param name="Kind">Which acquisition mechanism this entry represents.</param>
    /// <param name="Display">
    /// The primary label: a store's localized name (falling back to its resref), a recipe's
    /// <c>RecipeType</c> enum member name, a loot table id, or a quest id.
    /// </param>
    /// <param name="Detail">
    /// An optional secondary line - the store resref, the source file name, or similar. Null when
    /// there is nothing more useful to show than <see cref="Display"/> alone.
    /// </param>
    /// <param name="SourceResRef">
    /// The resref of the source blueprint itself (e.g. the store's .utm resref), when the source is
    /// a module resource the editor could jump to. Null for C#-only sources (recipes, loot tables,
    /// quests) which have no blueprint resref of their own.
    /// </param>
    public sealed record ItemSourceEntry(
        ItemSourceKind Kind,
        string Display,
        string? Detail,
        string? SourceResRef);
}
