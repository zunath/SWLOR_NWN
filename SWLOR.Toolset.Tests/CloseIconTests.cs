using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAssertions;
using NUnit.Framework;

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
    }
}
