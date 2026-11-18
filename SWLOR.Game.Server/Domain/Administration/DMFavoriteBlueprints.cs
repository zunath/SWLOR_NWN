using System;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// Turns a DM Creator blueprint into the favorite record a DM saves and compares the two. The record
    /// (<see cref="DMFavoriteBlueprint"/>) is stored data below Administration, so the reads that name a
    /// <see cref="DMBlueprint"/> live here.
    /// </summary>
    public static class DMFavoriteBlueprints
    {
        /// <summary>The favorite record that saves <paramref name="blueprint"/>.</summary>
        public static DMFavoriteBlueprint ToFavorite(this DMBlueprint blueprint)
        {
            return new DMFavoriteBlueprint
            {
                Category = blueprint.Category,
                Source = blueprint.Source,
                Resref = blueprint.Resref,
                Name = blueprint.Name
            };
        }

        /// <summary>
        /// Returns true when this favorite refers to the blueprint.
        /// </summary>
        public static bool Matches(this DMFavoriteBlueprint favorite, DMBlueprint blueprint)
        {
            return favorite.Category == blueprint.Category &&
                   favorite.Source == blueprint.Source &&
                   string.Equals(favorite.Resref, blueprint.Resref, StringComparison.OrdinalIgnoreCase);
        }
    }
}
