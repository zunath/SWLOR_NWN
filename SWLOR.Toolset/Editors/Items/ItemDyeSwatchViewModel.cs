using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>One selectable color in a dye channel's picker: the palette index, drawn.</summary>
    public sealed partial class ItemDyeSwatchViewModel : ObservableObject
    {
        public ItemDyeSwatchViewModel(int index, (byte R, byte G, byte B) color)
        {
            Index = index;
            Brush = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
        }

        /// <summary>The value written to the dye field - a builder never has to know it, but the tooltip says it.</summary>
        public int Index { get; }

        public IBrush Brush { get; }

        /// <summary>Index included so a specific dye can still be found, matched or reported by number.</summary>
        public string Tooltip => $"Color {Index}";

        [ObservableProperty]
        private bool _isSelected;
    }
}
