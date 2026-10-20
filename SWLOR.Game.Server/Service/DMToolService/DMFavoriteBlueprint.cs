namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// A blueprint a DM saved as a DM Creator favorite. The category, palette, and resref identify it; the name is
    /// only shown when the blueprint is no longer available.
    /// </summary>
    public class DMFavoriteBlueprint
    {
        public DMBlueprintCategory Category { get; set; }
        public DMBlueprintSource Source { get; set; }
        public string Resref { get; set; }
        public string Name { get; set; }

        public DMFavoriteBlueprint()
        {
            Resref = string.Empty;
            Name = string.Empty;
        }

        public DMFavoriteBlueprint(DMBlueprint blueprint)
        {
            Category = blueprint.Category;
            Source = blueprint.Source;
            Resref = blueprint.Resref;
            Name = blueprint.Name;
        }

        /// <summary>
        /// Returns true when this favorite refers to the blueprint.
        /// </summary>
        public bool Matches(DMBlueprint blueprint)
        {
            return Category == blueprint.Category &&
                   Source == blueprint.Source &&
                   string.Equals(Resref, blueprint.Resref, StringComparison.OrdinalIgnoreCase);
        }
    }
}
