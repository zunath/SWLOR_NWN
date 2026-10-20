using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// Adds and removes DM Creator favorites and builds the Favorites palette from them.
    /// </summary>
    public static class DMFavoriteList
    {
        /// <summary>
        /// The most favorites one DM can keep, across every blueprint type.
        /// </summary>
        public const int MaxFavorites = 500;

        /// <summary>
        /// The Favorites palette category for favorites whose blueprint no palette lists any more.
        /// </summary>
        public const string UnavailableCategory = "Unavailable";

        /// <summary>
        /// Returns true when the blueprint is one of the favorites.
        /// </summary>
        public static bool Contains(IEnumerable<DMFavoriteBlueprint> favorites, DMBlueprint blueprint)
        {
            return favorites.Any(favorite => favorite.Matches(blueprint));
        }

        /// <summary>
        /// Removes the blueprint when it is a favorite, otherwise adds it unless the list is full.
        /// </summary>
        /// <param name="favorites">The DM's favorites, changed in place.</param>
        /// <param name="blueprint">The blueprint to add or remove.</param>
        public static DMFavoriteChange Toggle(List<DMFavoriteBlueprint> favorites, DMBlueprint blueprint)
        {
            if (favorites.RemoveAll(favorite => favorite.Matches(blueprint)) > 0)
                return DMFavoriteChange.Removed;

            if (favorites.Count >= MaxFavorites)
                return DMFavoriteChange.Full;

            favorites.Add(new DMFavoriteBlueprint(blueprint));
            return DMFavoriteChange.Added;
        }

        /// <summary>
        /// Builds a palette of one type's favorites, sorted by name. Each favorite takes its current name and category
        /// from the palette that lists its resref, so renamed blueprints show their new names. A favorite no palette
        /// lists is kept under the Unavailable category with its saved name, so the DM can see and remove it.
        /// </summary>
        /// <param name="favorites">The DM's favorites of every type.</param>
        /// <param name="category">The blueprint type to list.</param>
        /// <param name="getPalette">Returns the type's Custom or Standard palette. Only palettes a favorite uses are read.</param>
        /// <param name="unavailable">Receives the blueprints of the favorites no palette lists.</param>
        public static DMPalette BuildPalette(
            IEnumerable<DMFavoriteBlueprint> favorites,
            DMBlueprintCategory category,
            Func<DMBlueprintSource, DMPalette> getPalette,
            ISet<DMBlueprint> unavailable)
        {
            var palettes = new Dictionary<DMBlueprintSource, DMPalette>();
            var lookups = new Dictionary<DMBlueprintSource, Dictionary<string, DMBlueprint>>();
            var available = new List<DMBlueprint>();
            var missing = new List<DMBlueprint>();
            var seen = new HashSet<(DMBlueprintSource, string)>();

            foreach (var favorite in favorites.Where(favorite => favorite.Category == category))
            {
                if (string.IsNullOrWhiteSpace(favorite.Resref) ||
                    !seen.Add((favorite.Source, favorite.Resref.ToLowerInvariant())))
                    continue;

                if (!lookups.TryGetValue(favorite.Source, out var lookup))
                {
                    var palette = getPalette(favorite.Source);
                    palettes[favorite.Source] = palette;
                    lookup = new Dictionary<string, DMBlueprint>(StringComparer.OrdinalIgnoreCase);
                    foreach (var blueprint in palette.Blueprints)
                        lookup.TryAdd(blueprint.Resref, blueprint);
                    lookups[favorite.Source] = lookup;
                }

                if (lookup.TryGetValue(favorite.Resref, out var listed))
                {
                    available.Add(listed);
                    continue;
                }

                var name = string.IsNullOrWhiteSpace(favorite.Name) ? favorite.Resref : favorite.Name;
                var gone = new DMBlueprint(favorite.Resref, name, UnavailableCategory, category, favorite.Source);
                missing.Add(gone);
                unavailable.Add(gone);
            }

            // Categories keep palette order, Custom before Standard, so the list reads like the palettes it came from.
            var usedPaths = new HashSet<string>(available.Select(blueprint => blueprint.CategoryPath), StringComparer.Ordinal);
            var categoryPaths = palettes
                .OrderBy(pair => pair.Key)
                .SelectMany(pair => pair.Value.CategoryPaths)
                .Where(usedPaths.Contains)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (missing.Count > 0)
                categoryPaths.Add(UnavailableCategory);

            var blueprints = available
                .Concat(missing)
                .OrderBy(blueprint => blueprint.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(blueprint => blueprint.Resref, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return new DMPalette(categoryPaths, blueprints);
        }
    }
}
