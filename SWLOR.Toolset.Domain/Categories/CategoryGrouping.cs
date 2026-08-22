namespace SWLOR.Toolset.Domain.Categories
{
    /// <summary>
    /// How a resource type's list is grouped in the explorer or palette. The default is
    /// <see cref="Automatic"/> so a fresh checkout is already organised before anyone has filed
    /// anything by hand.
    /// </summary>
    public enum CategoryGrouping
    {
        /// <summary>Derived from the resource's display name by <see cref="AutomaticGrouping"/>.</summary>
        Automatic,

        /// <summary>The user's own folders, from the sidecar.</summary>
        Folders,

        /// <summary>No grouping - one flat, name-sorted list.</summary>
        Flat
    }
}
