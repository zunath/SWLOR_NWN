using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Categories
{
    /// <summary>
    /// One resource that can be filed: its resref and its display name, if it has one.
    /// </summary>
    public readonly record struct SeedableResource(string ResRef, string? Name);

    /// <summary>
    /// Gives Module Contents' three sections a starting folder tree, derived from what the module's own
    /// names and resrefs already encode.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seeded once into the category sidecar rather than recomputed on every build, which is the whole
    /// point: a computed grouping cannot be renamed, nested or corrected, and a builder's first question
    /// about any automatic filing is "how do I fix this one". So these rules only have to be a good
    /// starting point, not the last word.
    /// </para>
    /// <para>
    /// <b>The three sections do not get the same treatment, because the three do not carry the same
    /// information.</b> Areas have display names that spell out place and function
    /// ("Tatooine - Anchorhead - North Entrance"), so their folders are read straight off the name.
    /// Dialogs and scripts have no display name at all - only a resref - so they are grouped by resref
    /// prefix, with the prefixes this module actually uses given real names below. Measured on the corpus:
    /// 362 of 443 area names carry a " - ", but only 168 of 609 dialogs are referenced by any blueprint's
    /// Conversation field, so there is no reliable route from a dialog to a place. Claiming to file
    /// dialogs "by planet" would mean guessing for most of them.
    /// </para>
    /// </remarks>
    public static class ModuleFolderSeeder
    {
        /// <summary>Separator the area naming convention uses between segments.</summary>
        public const string Separator = AutomaticGrouping.Separator;

        /// <summary>Prefix marking a reusable area that is not part of the live world.</summary>
        public const string PrefabMarker = "[Prefab]";

        /// <summary>Folder the prefab areas are collected under.</summary>
        public const string PrefabsFolder = "Prefabs";

        /// <summary>
        /// Folder for the engine-plumbing areas, which the module marks by starting their name with
        /// '*' ("*Character Rebuild", "*No Access", "*Hidden Access Area").
        /// </summary>
        public const string SystemFolder = "System";

        /// <summary>Folder for content that shipped with NWN or a third-party toolkit, not with SWLOR.</summary>
        public const string BaseGameFolder = "Base game";

        /// <summary>Marker the module puts in front of an engine-plumbing area's name.</summary>
        private const char SystemMarker = '*';

        /// <summary>
        /// Resref prefixes for dialogs, and the folder each belongs in. Two-level paths put a place under
        /// the planet it is on, so dialog folders read like the area folders where that is knowable.
        /// </summary>
        /// <remarks>
        /// Only prefixes the corpus actually uses in quantity, with counts measured at the time of
        /// writing. Anything not listed lands in Unsorted rather than being guessed at, which is the
        /// honest outcome for 332 dialogs whose resref is a single word ("bartender", "cardmaster").
        /// </remarks>
        private static readonly (string Prefix, string[] Path)[] DialogPrefixes =
        {
            // Places. veles is the Viscaran colony, mcdce is Mon Cala's Dac City.
            ("veles", new[] { "Viscara", "Veles" }),          // 42
            ("dan", new[] { "Dantooine" }),                   // 7
            ("tat", new[] { "Tatooine" }),                    // 7
            ("nar", new[] { "Nar Shaddaa" }),                 // 4
            ("mcdce", new[] { "Mon Cala", "Dac City" }),      // 5
            ("cz220", new[] { "CZ-220" }),                    // 2
            ("cz", new[] { "CZ-220" }),                       // 5
            ("zomb", new[] { "Abandoned Station" }),          // 2

            // Functions.
            ("cq", new[] { "Contract quests" }),              // 39
            ("rep", new[] { "Republic" }),                    // 24
            ("repbase", new[] { "Republic" }),                // 4
            ("rev", new[] { "Revan's crew" }),                // 17
            ("dt", new[] { "Cantinas & shops" }),             // 18
            ("start", new[] { "Character creation" }),        // 6

            // Stock BioWare content, which is not SWLOR's and is rarely what a builder wants.
            ("nw", new[] { BaseGameFolder }),                 // 32
            ("x0", new[] { BaseGameFolder }),
            ("x2", new[] { BaseGameFolder })
        };

        /// <summary>
        /// Resref prefixes for scripts. Scripts in this module are almost entirely imported toolkits, so
        /// they are grouped by where they came from - which is the thing that decides whether you may
        /// touch one.
        /// </summary>
        private static readonly (string Prefix, string[] Path)[] ScriptPrefixes =
        {
            ("dmfi", new[] { "DMFI toolkit" }),               // 45
            ("zep", new[] { "ZEP toolkit" }),                 // 19
            ("nw", new[] { BaseGameFolder }),                 // 17
            ("x0", new[] { BaseGameFolder }),                 // 2
            ("x2", new[] { BaseGameFolder }),                 // 2
            ("nbde", new[] { BaseGameFolder })
        };

        /// <summary>
        /// Files <paramref name="resources"/> into <paramref name="section"/>, creating folders as needed.
        /// Returns how many folders were created. Does nothing when the section already has folders - a
        /// builder's arrangement is never overwritten.
        /// </summary>
        public static int Seed(
            CategorySection section, ResourceType type, IEnumerable<SeedableResource> resources)
        {
            ArgumentNullException.ThrowIfNull(section);
            ArgumentNullException.ThrowIfNull(resources);

            if (section.Folders.Count > 0)
                return 0;

            var before = section.AllFolders().Count();

            foreach (var resource in resources)
            {
                var path = PathFor(type, resource);
                if (path.Count == 0)
                    continue;

                FolderAt(section, path).AddMember(resource.ResRef);
            }

            return section.AllFolders().Count() - before;
        }

        /// <summary>
        /// The folder path a resource belongs in, or empty for "leave it in Unsorted". Public so the rule
        /// can be asserted directly rather than only through a seeded section.
        /// </summary>
        public static IReadOnlyList<string> PathFor(ResourceType type, SeedableResource resource) => type switch
        {
            ResourceType.Area => AreaPath(resource.Name),
            ResourceType.Dlg => PrefixPath(resource.ResRef, DialogPrefixes),
            ResourceType.Nss => PrefixPath(resource.ResRef, ScriptPrefixes),
            _ => Array.Empty<string>()
        };

        /// <summary>
        /// The label to show for a resource once it is inside its folder - the part of the name its
        /// folders do not already say.
        /// </summary>
        public static string LeafLabel(ResourceType type, SeedableResource resource)
        {
            if (type != ResourceType.Area || string.IsNullOrWhiteSpace(resource.Name))
                return resource.ResRef;

            var segments = Segments(resource.Name);
            return segments.Count > 0 ? segments[^1] : resource.Name.Trim();
        }

        /// <summary>
        /// An area's folders, read off its display name. Prefabs go under one <see cref="PrefabsFolder"/>
        /// tree rather than being scattered as "[Prefab] Korriban" beside the real Korriban; '*'-marked
        /// plumbing goes under <see cref="SystemFolder"/>; everything else nests by its own segments, so
        /// the last segment is the row and the ones before it are the path.
        /// </summary>
        private static IReadOnlyList<string> AreaPath(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return Array.Empty<string>();

            var name = displayName.Trim();

            if (name.StartsWith(PrefabMarker, StringComparison.OrdinalIgnoreCase))
            {
                var remainder = name[PrefabMarker.Length..].Trim();
                var inner = Segments(remainder);

                // "[Prefab] Korriban - Tomb" -> Prefabs/Korriban, leaf "Tomb".
                // "[Prefab] Ebon Hawk" -> Prefabs, leaf "Ebon Hawk".
                return new[] { PrefabsFolder }.Concat(inner.Take(Math.Max(0, inner.Count - 1))).ToList();
            }

            if (name.StartsWith(SystemMarker))
                return new[] { SystemFolder };

            var segments = Segments(name);

            // A name with no separator says nothing about where it belongs, so it stays in Unsorted
            // rather than becoming a folder of one.
            return segments.Count < 2
                ? Array.Empty<string>()
                : segments.Take(segments.Count - 1).ToList();
        }

        /// <summary>The path whose prefix matches, longest prefix first so "repbase" beats "rep".</summary>
        private static IReadOnlyList<string> PrefixPath(
            string resRef, (string Prefix, string[] Path)[] table)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                return Array.Empty<string>();

            var token = resRef.Split('_')[0];

            foreach (var (prefix, path) in table.OrderByDescending(entry => entry.Prefix.Length))
            {
                if (string.Equals(token, prefix, StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Splits a display name on the convention's separator, dropping empty segments. Each segment is
        /// sanitized, because these become folder names and they come from a blueprint's name rather than
        /// from anything a builder typed here - one holding a path separator must not take the whole seed
        /// down with it. A segment left with nothing usable is dropped, as an empty one already is.
        /// </summary>
        private static IReadOnlyList<string> Segments(string name) =>
            name.Split(Separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(CategoryFolder.Sanitize)
                .OfType<string>()
                .ToList();

        /// <summary>Walks (creating as needed) to the folder at a path.</summary>
        private static CategoryFolder FolderAt(CategorySection section, IReadOnlyList<string> path)
        {
            var current = section.Folders
                .FirstOrDefault(folder => string.Equals(folder.Name, path[0], StringComparison.OrdinalIgnoreCase))
                ?? section.AddFolder(path[0]);

            for (var i = 1; i < path.Count; i++)
            {
                var name = path[i];
                current = current.Children
                    .FirstOrDefault(child => string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
                    ?? current.AddChild(name);
            }

            return current;
        }
    }
}
