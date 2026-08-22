namespace SWLOR.Toolset.Domain.GameData.Resources
{
    /// <summary>
    /// One indexed custom-content layer. Implementations cover both the repository's loose HAK
    /// source folders and the packed .hak archives the game actually mounts.
    /// </summary>
    internal interface IHakResourceCatalog
    {
        string SourcePath { get; }

        DateTime ContentVersionUtc { get; }

        IEnumerable<ResourceIdentity> Resources { get; }

        bool TryGetBytes(ResourceIdentity identity, out byte[] bytes);

        string Describe(ResourceIdentity identity);
    }
}
