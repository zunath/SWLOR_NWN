using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Dock.Avalonia.Controls;
using Dock.Model.Mvvm.Controls;
using FluentAssertions;
using NUnit.Framework;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Keeps every close surface on the Toolset-owned vector. Dock has separate geometry hooks for
    /// document tabs and tool/MDI chrome; missing either silently restores its package default.
    /// </summary>
    public class CloseIconTests
    {
        [AvaloniaTest]
        public void EveryCloseSurfaceUsesTheToolsetGeometry()
        {
            var app = Application.Current!;

            app.TryGetResource("ToolsetCloseIconGeometry", ThemeVariant.Dark, out var generated)
                .Should().BeTrue();
            generated.Should().BeOfType<StreamGeometry>();

            app.TryGetResource("DockIconCloseGeometry", ThemeVariant.Dark, out var documentClose)
                .Should().BeTrue();
            app.TryGetResource("DockToolIconCloseGeometry", ThemeVariant.Dark, out var toolClose)
                .Should().BeTrue();

            documentClose.Should().BeSameAs(generated);
            toolClose.Should().BeSameAs(generated);
        }

        [AvaloniaTest]
        public void RenderedDocumentTabClosePathHasVisibleToolsetArtwork()
        {
            var document = new Document
            {
                Id = "Document",
                Title = "Document"
            };
            var strip = new DocumentTabStrip
            {
                Width = 200,
                ItemsSource = new[] { document },
                SelectedItem = document
            };
            strip.DataTemplates.Add(
                new FuncDataTemplate<Document>((_, _) => new Border()));
            var window = new Window
            {
                Width = 240,
                Height = 80,
                Content = strip
            };

            window.Show();

            var closePath = strip.GetVisualDescendants()
                .OfType<AvaloniaPath>()
                .Single(path => path.Name == "PART_ClosePath");
            Application.Current!.TryGetResource(
                    "ToolsetCloseIconGeometry",
                    ThemeVariant.Dark,
                    out var generated)
                .Should().BeTrue();

            closePath.Data.Should().BeSameAs(generated);
            closePath.Fill.Should().NotBeNull();
            closePath.Fill.Should().NotBe(Brushes.Transparent);
            closePath.Bounds.Width.Should().BeGreaterThan(4);
            closePath.Bounds.Height.Should().BeGreaterThan(4);

            window.Close();
        }
    }
}
