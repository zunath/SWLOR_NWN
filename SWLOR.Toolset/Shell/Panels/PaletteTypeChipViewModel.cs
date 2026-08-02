using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// One blueprint type in the palette's type row: its symbol, with the friendly plural as the tooltip.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Icons rather than words, which is what lets all eight types sit in one row of a narrow panel. The
    /// text row could not: three fitted, the rest hid behind a More... button that expanded the row
    /// sideways until it ran off the panel. Aurora's palette was a strip of type icons for the same
    /// reason, so the shape is also the familiar one.
    /// </para>
    /// <para>
    /// The symbol is the same one a tile falls back to when its blueprint has no artwork, drawn at chip
    /// size - so the thing standing for "placeable" in the type row is the thing standing for "placeable"
    /// in the grid.
    /// </para>
    /// </remarks>
    public partial class PaletteTypeChipViewModel : ObservableObject
    {
        public PaletteTypeChipViewModel(ResourceType type, Bitmap? icon)
        {
            Type = type;
            Label = type.DisplayName();
            Icon = icon;
        }

        private PaletteTypeChipViewModel(string label, Bitmap? icon)
        {
            Type = null;
            Label = label;
            Icon = icon;
        }

        /// <summary>
        /// The Tiles chip. It carries no <see cref="ResourceType"/> because a tile is not a module
        /// resource - it is a row in the open area's tileset - which is exactly why the palette has to
        /// treat this entry differently from the eight blueprint types.
        /// </summary>
        public static PaletteTypeChipViewModel ForTiles(Bitmap? icon) => new("Tiles", icon);

        /// <summary>Null for the Tiles chip; a blueprint type otherwise.</summary>
        public ResourceType? Type { get; }

        public bool IsTiles => Type == null;

        /// <summary>The plural name. Shown as a tooltip, since the chip itself is the icon.</summary>
        public string Label { get; }

        /// <summary>Null only when game data is unavailable; the view falls back to the initial letter.</summary>
        public Bitmap? Icon { get; }

        public bool HasIcon => Icon != null;

        /// <summary>Stand-in when there is no icon, so the row never collapses to blank buttons.</summary>
        public string Initial => Label.Length > 0 ? Label[..1] : "?";

        [ObservableProperty]
        private bool _isSelected;
    }
}
