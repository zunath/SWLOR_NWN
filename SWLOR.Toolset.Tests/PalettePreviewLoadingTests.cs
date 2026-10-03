using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
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
        public void HostPaletteEmbedsTheSharedPalettePresentation()
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            var palette = new PaletteViewModel(workspace, new CategoryService(workspace, log), log);
            var view = new PaletteView { DataContext = palette };
            var window = new Window { Width = 1100, Height = 700, Content = view };

            try
            {
                window.Show();
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();

                Assert.That(view.Content, Is.TypeOf<Nwn.Toolset.Avalonia.Palettes.Views.PaletteView>());
                var sharedView = (Nwn.Toolset.Avalonia.Palettes.Views.PaletteView)view.Content!;
                Assert.That(sharedView.DataContext, Is.SameAs(palette.PresentationState));
            }
            finally
            {
                window.Close();
            }
        }
    }
}