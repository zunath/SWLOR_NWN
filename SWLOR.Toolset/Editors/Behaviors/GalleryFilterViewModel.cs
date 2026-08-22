using CommunityToolkit.Mvvm.ComponentModel;

namespace SWLOR.Toolset.Editors.Behaviors
{
    /// <summary>One value in a shared gallery filter; null means all values.</summary>
    public sealed record GalleryFilterOption(string? ValueKey, string Display)
    {
        public override string ToString() => Display;
    }

    /// <summary>One facet control discovered from the choices published to a shared gallery.</summary>
    public sealed class GalleryFilterViewModel : ObservableObject
    {
        private GalleryFilterOption _selectedOption;
        private readonly Action _selectionChanged;

        public string GroupKey { get; }
        public string Label { get; }
        public IReadOnlyList<GalleryFilterOption> Options { get; }

        public GalleryFilterOption SelectedOption
        {
            get => _selectedOption;
            set
            {
                if (!SetProperty(ref _selectedOption, value))
                    return;

                _selectionChanged();
            }
        }

        public GalleryFilterViewModel(
            string groupKey,
            string label,
            IReadOnlyList<GalleryFilterOption> options,
            Action selectionChanged)
        {
            GroupKey = groupKey;
            Label = label;
            Options = options;
            _selectionChanged = selectionChanged;
            _selectedOption = options.First();
        }
    }

    public enum GallerySortMode
    {
        Default,
        NameAscending,
        NameDescending,
        IdAscending,
        IdDescending
    }

    /// <summary>One ordering offered by every shared visual gallery.</summary>
    public sealed record GallerySortOption(GallerySortMode Mode, string Display)
    {
        public override string ToString() => Display;
    }
}
