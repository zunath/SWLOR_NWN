namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// The effective texture maps resolved for one mesh bitmap/material name: the diffuse map
    /// plus its optional normal and specular companions. Produced by
    /// <see cref="MaterialResolver.ResolveMaterialMaps"/>; names are texture resrefs, not yet
    /// loaded or verified decodable.
    /// </summary>
    public sealed class MaterialMaps
    {
        public required string Diffuse { get; init; }

        /// <summary>Tangent-space normal map resref, or null when the material has none.</summary>
        public string? Normal { get; init; }

        /// <summary>Specular map resref, or null when the material has none.</summary>
        public string? Specular { get; init; }

        /// <summary>Roughness map resref, or null when the material has none.</summary>
        public string? Roughness { get; init; }
    }
}
