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
        /// Clicking a tile arms placement: the object then follows the cursor in the area view until a
        /// click puts it down. That is the default because it is what a builder does with a palette nine
        /// times in ten; editing is on the tile's ellipsis and its right-click menu, so the common action
        /// needs no aim and the rare one is still one click away.
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

        // Right-clicking selects what was right-clicked before the menu opens. Avalonia does not do this
        // for us, and without it a context-menu command would act on whatever happened to be selected
        // before - the classic way to delete the wrong thing.

        private void OnTileContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (DataContext is PaletteViewModel viewModel &&
                sender is Control { DataContext: PaletteTileViewModel tile })
            {
                viewModel.SelectedTile = tile;
            }
        }

        private void OnCategoryContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (DataContext is PaletteViewModel viewModel &&
                sender is Control { DataContext: CategoryRowViewModel row })
            {
                viewModel.SelectedRow = row;
            }
        }
    }
}
