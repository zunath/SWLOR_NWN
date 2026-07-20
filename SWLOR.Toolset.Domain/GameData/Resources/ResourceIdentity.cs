using Radoub.Formats.Common;

namespace SWLOR.Toolset.Domain.GameData.Resources
{
    /// <summary>
    /// Identifies a game resource by lowercase resref and Aurora resource type, mirroring how
    /// NWN itself keys resources (KEY/BIF, ERF/HAK, and loose override files are all indexed by
    /// resref+type, case-insensitively).
    ///
    /// The extension&lt;-&gt;type mapping is backed by <see cref="ResourceTypes"/> (Radoub.Formats.
    /// Common) for every extension WP2.3 needs - mdl, wok, set, tga, dds, txi, plt, 2da, tlk, itp,
    /// wav all match Radoub's table exactly. The one gap is "mtr" (NWN:EE material files, type
    /// 2072 per SWLOR.NWN.API's own <c>ResType</c> enum - Radoub.Formats.Common.ResourceTypes does
    /// not define it), which is patched in locally rather than re-deriving Radoub's whole table.
    /// </summary>
    public readonly struct ResourceIdentity : IEquatable<ResourceIdentity>
    {
        // Resource types NWN:EE defines that Radoub.Formats.Common.ResourceTypes does not (yet)
        // expose, keyed by lowercase extension without a leading dot.
        private static readonly IReadOnlyDictionary<string, ushort> ExtraExtensionToType =
            new Dictionary<string, ushort>
            {
                ["mtr"] = 2072
            };

        private static readonly IReadOnlyDictionary<ushort, string> ExtraTypeToExtension =
            new Dictionary<ushort, string>
            {
                [2072] = "mtr"
            };

        public string ResRef { get; }
        public ushort ResourceType { get; }

        public ResourceIdentity(string resRef, ushort resourceType)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                throw new ArgumentException("ResRef must not be empty.", nameof(resRef));

            // Normalized once here so every downstream comparison/dictionary lookup can use a
            // plain ordinal comparison instead of paying for OrdinalIgnoreCase repeatedly.
            ResRef = resRef.Trim().ToLowerInvariant();
            ResourceType = resourceType;
        }

        /// <summary>
        /// Build an identity from a loose file name such as "tde01.set" or "c_barract.001.mtr".
        /// </summary>
        public static ResourceIdentity FromFileName(string fileName)
        {
            var resRef = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            return new ResourceIdentity(resRef, TypeFromExtension(extension));
        }

        /// <summary>
        /// Resolve a resource type from a file extension (with or without a leading dot).
        /// Returns <see cref="ResourceTypes.Invalid"/> for unrecognized extensions.
        /// </summary>
        public static ushort TypeFromExtension(string extension)
        {
            var ext = extension.TrimStart('.').ToLowerInvariant();
            if (ExtraExtensionToType.TryGetValue(ext, out var extraType))
                return extraType;

            return ResourceTypes.FromExtension(ext);
        }

        /// <summary>
        /// Resolve the bare (no leading dot) file extension for a resource type.
        /// </summary>
        public static string ExtensionFromType(ushort resourceType)
        {
            if (ExtraTypeToExtension.TryGetValue(resourceType, out var extraExtension))
                return extraExtension;

            return ResourceTypes.GetExtension(resourceType).TrimStart('.');
        }

        public string Extension => ExtensionFromType(ResourceType);

        public bool Equals(ResourceIdentity other) =>
            ResourceType == other.ResourceType && string.Equals(ResRef, other.ResRef, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is ResourceIdentity other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(ResRef, ResourceType);

        public override string ToString() => $"{ResRef}.{Extension}";

        public static bool operator ==(ResourceIdentity left, ResourceIdentity right) => left.Equals(right);

        public static bool operator !=(ResourceIdentity left, ResourceIdentity right) => !left.Equals(right);
    }
}
