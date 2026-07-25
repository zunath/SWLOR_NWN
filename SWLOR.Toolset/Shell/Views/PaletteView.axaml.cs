using Avalonia.Controls;
using Avalonia.Input;
using SWLOR.Toolset.Shell.Panels;

namespace SWLOR.Toolset.Shell.Views
{
    public partial class PaletteView : UserControl
    {
        public PaletteView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Clicking a tile arms placement. That is the default because it is what a builder does with a
        /// palette nine times in ten; editing is on the tile's ellipsis and its right-click menu, so the
        /// common action needs no aim and the rare one is still one click away.
        /// </summary>
        private void OnTileTapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is not PaletteViewModel viewModel)
                return;

            if (sender is not Control { DataContext: PaletteTileViewModel tile })
                return;

            viewModel.SelectedTile = tile;
            viewModel.PlaceCommand.Execute(tile);
        }
    }
}
