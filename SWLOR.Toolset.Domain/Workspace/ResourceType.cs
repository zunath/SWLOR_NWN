namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// The module resource kinds a <see cref="ModuleWorkspace"/> knows how to enumerate and load:
    /// areas (.are, paired with .git/.gic) and every blueprint type this package supports.
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
        Utw
    }

    /// <summary>
    /// File-naming conventions for <see cref="ResourceType"/>: the module subfolder name and the
    /// blueprint/area file extension are identical for every type this package supports (e.g.
    /// "utc" folder holds "*.utc.json" files), so one string covers both.
    /// </summary>
    public static class ResourceTypeExtensions
    {
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
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown resource type.")
            };
        }

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
                ResourceType.Uts => "Sound Sets",
                ResourceType.Utw => "Waypoints",
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
                ResourceType.Uts => "Sound Set",
                ResourceType.Utw => "Waypoint",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown resource type.")
            };
        }
    }
}
