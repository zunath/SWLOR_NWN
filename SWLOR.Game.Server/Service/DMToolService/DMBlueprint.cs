namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// A blueprint the DM creator can spawn, as listed in its palette.
    /// </summary>
    public class DMBlueprint
    {
        public string Resref { get; }
        public string Name { get; }
        public string CategoryPath { get; }
        public DMBlueprintCategory Category { get; }
        public DMBlueprintSource Source { get; }

        /// <summary>
        /// The challenge rating the palette lists for creatures, or null for other blueprints.
        /// </summary>
        public float? ChallengeRating { get; }

        /// <summary>
        /// The faction name the palette lists for creatures, or an empty string.
        /// </summary>
        public string Faction { get; }

        public DMBlueprint(
            string resref,
            string name,
            string categoryPath,
            DMBlueprintCategory category,
            DMBlueprintSource source,
            float? challengeRating = null,
            string faction = "")
        {
            Resref = resref;
            Name = name;
            CategoryPath = categoryPath;
            Category = category;
            Source = source;
            ChallengeRating = challengeRating;
            Faction = faction ?? string.Empty;
        }

        /// <summary>
        /// Returns true when the blueprint sits in the category or one of its sub-categories.
        /// </summary>
        /// <param name="categoryPath">A category path from the palette, or an empty string for every category.</param>
        public bool IsInCategory(string categoryPath)
        {
            return string.IsNullOrEmpty(categoryPath) ||
                   CategoryPath == categoryPath ||
                   CategoryPath.StartsWith(categoryPath + DMPaletteReader.PathSeparator, StringComparison.Ordinal);
        }
    }
}
