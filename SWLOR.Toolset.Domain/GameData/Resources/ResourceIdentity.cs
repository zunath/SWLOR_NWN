using SWLOR.NWN.Formats.Common;

namespace SWLOR.Toolset.Domain.GameData.Resources
{
    /// <summary>
    /// Identifies a game resource by lowercase resref and Aurora resource type, mirroring how
    /// NWN itself keys resources (KEY/BIF, ERF/HAK, and loose override files are all indexed by
    /// resref+type, case-insensitively).
    ///
    /// The extension&lt;-&gt;type mapping is backed by <see cref="ResourceTypes"/> in the standalone
    /// formats library, including NWN:EE resource types such as MTR.
    /// </summary>
    public readonly struct ResourceIdentity : IEquatable<ResourceIdentity>
    {
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
            return ResourceTypes.FromExtension(extension);
        }

        /// <summary>
        /// Resolve the bare (no leading dot) file extension for a resource type.
        /// </summary>
        public static string ExtensionFromType(ushort resourceType)
        {
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
