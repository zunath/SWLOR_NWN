using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using SWLOR.Toolset.Shell.Panels;

namespace SWLOR.Toolset.Shell.Views
{
    public partial class PaletteView : UserControl
    {
        /// <summary>The two star-sized rows the category/objects divider trades height between.</summary>
        private const int CategoryRow = 5;
        private const int ObjectsRow = 7;

        public PaletteView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Puts the category/objects divider back where it was left. Applied on load rather than through a
        /// binding because the Grid owns the live row heights - a binding would have to fight the splitter
        /// for them.
        /// </summary>
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            if (DataContext is not PaletteViewModel viewModel)
                return;

            var proportion = viewModel.CategoryProportion;

            // 0 means nothing was saved, and anything at the extremes would restore a panel with one of
            // its two halves collapsed to nothing - which reads as a missing tree or a missing grid.
            if (proportion < 0.05 || proportion > 0.95)
                return;

            var rows = PaletteRows.RowDefinitions;
            rows[CategoryRow].Height = new GridLength(proportion, GridUnitType.Star);
            rows[ObjectsRow].Height = new GridLength(1 - proportion, GridUnitType.Star);
        }

        /// <summary>
        /// Records where the divider was let go, as a share of the height the two rows share - a
        /// proportion rather than a pixel height, so it means the same thing in a panel of any size.
        /// </summary>
        private void OnCategorySplitterDragCompleted(object? sender, VectorEventArgs e)
        {
            if (DataContext is not PaletteViewModel viewModel)
                return;

            var rows = PaletteRows.RowDefinitions;
            var category = rows[CategoryRow].ActualHeight;
            var objects = rows[ObjectsRow].ActualHeight;
            var total = category + objects;

            if (total <= 0)
                return;

            viewModel.CategoryProportion = category / total;
        }

        // ----- preview loading -----
        //
        // A cell asks for its image when the virtualizing panel realizes it, not when its category
        // opens. Opening a 2,000-object category still fetches only the viewport and its one-screen
        // buffer, but every input path is covered: initial layout, mouse wheel, keyboard navigation and
        // dragging the scrollbar thumb.

        private void OnTileLoaded(object? sender, RoutedEventArgs e)
        {
            if (sender is not Control { DataContext: PaletteTileViewModel tile } ||
                DataContext is not PaletteViewModel viewModel)
            {
                return;
            }

            viewModel.EnsurePreview(tile);
        }

        /// <summary>
        /// Clicking a tile arms placement: the object then follows the cursor in the area view until a
        /// click puts it down. That is the default because it is what a builder does with a palette nine
        /// times in ten; everything else is on the tile's menu, which its ellipsis and its right-click
        /// both open, so the common action needs no aim and the rest is still one click away.
        /// </summary>
        private void OnTileTapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is not PaletteViewModel viewModel)
                return;

            if (sender is not Control { DataContext: PaletteTileViewModel tile })
                return;

            // A tap on the tile's own ellipsis is a tap on the tile as far as the gesture is
            // concerned, so without this, opening the menu also arms placement.
            if (IsWithinButton(e.Source as Visual, sender as Visual))
                return;

            viewModel.SelectedTile = tile;
            viewModel.PlaceCommand.Execute(tile);
        }

        /// <summary>
        /// Whether <paramref name="source"/> sits inside a button below <paramref name="root"/>. Walks
        /// the visual tree: what a tap reports is whatever the button's own template put on screen,
        /// which the logical tree does not lead back out of.
        /// </summary>
        private static bool IsWithinButton(Visual? source, Visual? root)
        {
            for (var node = source; node != null && node != root; node = node.GetVisualParent())
            {
                if (node is Button)
                    return true;
            }

            return false;
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

        /// <summary>
        /// The tile's ellipsis opens the tile's own context menu - the same menu, selecting the same
        /// tile first, so the visible handle and the right-click reach one set of actions rather than
        /// two. Left at the default pointer placement, which puts the menu under the ellipsis.
        /// </summary>
        private void OnTileMenuButtonClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Control control)
                return;

            if (DataContext is PaletteViewModel viewModel &&
                control.DataContext is PaletteTileViewModel tile)
            {
                viewModel.SelectedTile = tile;
            }

            // The menu hangs off the tile's root Border, several levels up from the button. Walking
            // for the first ancestor that owns one keeps this working if the cell's markup changes.
            for (var owner = control.Parent as Control; owner != null; owner = owner.Parent as Control)
            {
                if (owner.ContextMenu is not { } menu)
                    continue;

                menu.Open(owner);
                e.Handled = true;
                return;
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
