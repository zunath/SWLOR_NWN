using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// One Source-tab group: a fixed kind title ("Store", "Recipe", ...) and the entries under it.
    /// Only built for kinds that actually have at least one entry - see
    /// <see cref="ItemSourceSectionViewModel.EmptyKinds"/> for the rest.
    /// </summary>
    public sealed class ItemSourceGroupViewModel
    {
        public string Title { get; }

        public IReadOnlyList<ItemSourceEntry> Entries { get; }

        public ItemSourceGroupViewModel(string title, IReadOnlyList<ItemSourceEntry> entries)
        {
            Title = title;
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }
    }
}
