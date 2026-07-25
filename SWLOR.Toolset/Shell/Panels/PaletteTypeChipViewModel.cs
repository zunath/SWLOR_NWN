using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// One blueprint type in the palette's type row, labelled with its friendly plural.
    /// </summary>
    /// <remarks>
    /// Deliberately carries no count. "8,355 placeables" answers a question nobody asks, whereas a count
    /// on a <em>category</em> answers a real one - is this folder worth opening - so counts live there
    /// instead.
    /// </remarks>
    public partial class PaletteTypeChipViewModel : ObservableObject
    {
        public PaletteTypeChipViewModel(ResourceType type)
        {
            Type = type;
            Label = type.DisplayName();
        }

        public ResourceType Type { get; }

        public string Label { get; }

        [ObservableProperty]
        private bool _isSelected;
    }
}
