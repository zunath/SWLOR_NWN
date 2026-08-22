namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// The module resource kinds a <see cref="ModuleWorkspace"/> knows how to enumerate and load:
    /// areas (.are, paired with .git/.gic), the blueprint types, dialogs, and scripts.
    /// </summary>
    public enum ResourceType
    {
        Area,
        Utc,
        Uti,
        Utp,
        Utd,
        Utm,
        Utt,
        Uts,
        Utw,

        /// <summary>A dialog (.dlg). Stored like the blueprints - nwn_gff JSON under Module/dlg.</summary>
        Dlg,

        /// <summary>
        /// A NWScript source file (.nss). The one type that is NOT nwn_gff JSON: these are plain text,
        /// so they live at Module/nss/&lt;resref&gt;.nss with no second extension.
        /// </summary>
        Nss
    }

    /// <summary>
    /// File-naming conventions for <see cref="ResourceType"/>: the module subfolder name matches the
    /// resource extension for every type (e.g. the "utc" folder holds "utc" resources), so one string
    /// covers both. Whether a file carries a further ".json" suffix is a separate question -
    /// see <see cref="IsJsonEncoded"/>.
    /// </summary>
    public static class ResourceTypeExtensions
    {
        /// <summary>
        /// The blueprint types a palette offers, in the Aurora toolset's own order.
        /// </summary>
        /// <remarks>
        /// Alphabetical by plural name, which is how Aurora listed them and therefore where a builder's
        /// hand already goes. Two of Aurora's entries are not in this list, for two different reasons.
        /// Tiles leads the palette's type row but is not a <see cref="ResourceType"/> at all - a tile is a
        /// row in the open area's tileset rather than a module resource. Encounters are gone outright:
        /// SWLOR uses its own spawn system, so the module carries no <c>.ute</c> blueprints and no area
        /// places an encounter.
        /// </remarks>
        public static readonly IReadOnlyList<ResourceType> PaletteOrder = new[]
        {
            ResourceType.Utc, ResourceType.Utd, ResourceType.Uti, ResourceType.Utm,
            ResourceType.Utp, ResourceType.Uts, ResourceType.Utt, ResourceType.Utw
        };

        public static string Extension(this ResourceType type)
        {
            return type switch
            {
                ResourceType.Area => "are",
                ResourceType.Utc => "utc",
                ResourceType.Uti => "uti",
                ResourceType.Utp => "utp",
                ResourceType.Utd => "utd",
                ResourceType.Utm => "utm",
                ResourceType.Utt => "utt",
                ResourceType.Uts => "uts",
                ResourceType.Utw => "utw",
                ResourceType.Dlg => "dlg",
                ResourceType.Nss => "nss",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown resource type.")
            };
        }

        /// <summary>
        /// Whether this resource is stored as unpacked nwn_gff JSON (so the file is
        /// "&lt;resref&gt;.&lt;ext&gt;.json") rather than in its own native format.
        /// </summary>
        /// <remarks>
        /// Everything in an unpacked module is GFF and therefore JSON, except NWScript source, which is
        /// text and was never GFF to begin with. Callers that read or write module files must ask this
        /// rather than assuming the double extension - assuming it is how a scripts folder ends up
        /// looking empty.
        /// </remarks>
        public static bool IsJsonEncoded(this ResourceType type) => type != ResourceType.Nss;

        /// <summary>
        /// The inverse of <see cref="Extension"/>, for reading a resource type back out of a file that
        /// keys things by extension (the category sidecar does). Case-insensitive; false when unknown,
        /// so an unrecognised key can be skipped rather than throwing.
        /// </summary>
        public static bool TryFromExtension(string? extension, out ResourceType type)
        {
            foreach (var candidate in Enum.GetValues<ResourceType>())
            {
                if (string.Equals(candidate.Extension(), extension, StringComparison.OrdinalIgnoreCase))
                {
                    type = candidate;
                    return true;
                }
            }

            type = default;
            return false;
        }

        /// <summary>
        /// The plural, human-readable name for a whole collection of this resource kind, for
        /// category lists and section headers. Player-of-the-toolset facing surfaces use these
        /// instead of the raw three-letter file extensions the enum is named after - nobody
        /// building content thinks in "utm", they think in "Merchants".
        /// </summary>
        public static string DisplayName(this ResourceType type)
        {
            return type switch
            {
                ResourceType.Area => "Areas",
                ResourceType.Utc => "Creatures",
                ResourceType.Uti => "Items",
                ResourceType.Utp => "Placeables",
                ResourceType.Utd => "Doors",
                ResourceType.Utm => "Merchants",
                ResourceType.Utt => "Triggers",
                // "Sounds", not "Sound Sets": a .uts is a placed ambient sound. The thing actually called
                // a sound set is a creature's SoundSetFile, a soundset.2da row, and is not a blueprint.
                ResourceType.Uts => "Sounds",
                ResourceType.Utw => "Waypoints",
                ResourceType.Dlg => "Dialogs",
                ResourceType.Nss => "Scripts",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown resource type.")
            };
        }

        /// <summary>
        /// The singular form of <see cref="DisplayName"/>, for surfaces that name one resource
        /// (search result rows, the Properties header, single-resource log lines).
        /// </summary>
        public static string SingularDisplayName(this ResourceType type)
        {
            return type switch
            {
                ResourceType.Area => "Area",
                ResourceType.Utc => "Creature",
                ResourceType.Uti => "Item",
                ResourceType.Utp => "Placeable",
                ResourceType.Utd => "Door",
                ResourceType.Utm => "Merchant",
                ResourceType.Utt => "Trigger",
                ResourceType.Uts => "Sound",
                ResourceType.Utw => "Waypoint",
                ResourceType.Dlg => "Dialog",
                ResourceType.Nss => "Script",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown resource type.")
            };
        }
    }
}
