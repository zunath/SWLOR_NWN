using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Shell.Views;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Preview requests follow tile realization, independent of how the palette reached that tile.
    /// </summary>
    public class PalettePreviewLoadingTests
    {
        [AvaloniaTest]
        public void ARealizedTileRequestsItsPreviewImmediately()
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            var palette = new PaletteViewModel(workspace, new CategoryService(workspace, log), log);
            var tile = new PaletteTileViewModel("test_item", "Test Item", null);
            var view = new PaletteView { DataContext = palette };
            var realizedCell = new Border { DataContext = tile };

            // Invoke the XAML event handler directly so the assertion does not depend on a later
            // EffectiveViewportChanged notification. That notification may follow initial layout and
            // wheel scrolling, but is the event scrollbar-thumb jumps can outrun.
            var loaded = typeof(PaletteView).GetMethod(
                "OnTileLoaded",
                BindingFlags.Instance | BindingFlags.NonPublic);

            loaded.Should().NotBeNull("the tile template must keep its Loaded handler");
            loaded!.Invoke(view, new object?[] { realizedCell, new RoutedEventArgs() });

            tile.PreviewRequested.Should().BeTrue(
                "realization itself must request the preview for every scrolling input path");
        }
    }
}
