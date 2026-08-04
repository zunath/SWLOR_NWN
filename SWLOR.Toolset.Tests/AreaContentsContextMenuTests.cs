using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Shell.Views;

namespace SWLOR.Toolset.Tests
{
    /// <summary>Exercises the real pointer-to-popup path for Area Contents rows.</summary>
    public sealed class AreaContentsContextMenuTests
    {
        [AvaloniaTest]
        public void RightClickingAnInstanceRowOpensItsPropertiesMenu() =>
            RightClickingARowMatchesItsPropertiesMenu(
                AreaContentsNodeKind.Instance, "Open properties...");

        [AvaloniaTest]
        public void RightClickingAGroupRowOpensItsFirstInstancePropertiesMenu() =>
            RightClickingARowMatchesItsPropertiesMenu(
                AreaContentsNodeKind.Group, "Open first instance properties...");

        [AvaloniaTest]
        public void RightClickingAKindHeadingDoesNotOpenAStalePropertiesMenu() =>
            RightClickingARowMatchesItsPropertiesMenu(AreaContentsNodeKind.Kind, null);

        private static void RightClickingARowMatchesItsPropertiesMenu(
            AreaContentsNodeKind kind,
            string? expectedLabel)
        {
            var row = new AreaContentsNodeViewModel(
                kind,
                ResourceType.Utc,
                "Test creature",
                depth: 1)
            {
                Indices = kind == AreaContentsNodeKind.Group ? new[] { 0, 1 } : new[] { 0 }
            };
            var viewModel = new AreaContentsViewModel();
            viewModel.Rows.Add(row);

            var view = new AreaContentsView { DataContext = viewModel };
            var window = new Window { Content = view, Width = 400, Height = 300 };
            window.Show();

            try
            {
                var rowSurface = view.GetVisualDescendants()
                    .OfType<Grid>()
                    .Single(control => ReferenceEquals(control.DataContext, row) && control.ContextMenu != null);
                var rowContainer = rowSurface.FindAncestorOfType<ListBoxItem>()!;
                var point = rowContainer.TranslatePoint(
                    new Point(rowContainer.Bounds.Width - 2, rowContainer.Bounds.Height / 2),
                    window)!.Value;

                window.MouseMove(point, RawInputModifiers.None);
                window.MouseDown(point, MouseButton.Right, RawInputModifiers.RightMouseButton);
                window.MouseUp(point, MouseButton.Right, RawInputModifiers.None);

                rowSurface.ContextMenu!.IsOpen.Should().Be(expectedLabel != null,
                    "only placement-bearing rows may expose the properties action");
                if (expectedLabel != null)
                {
                    rowSurface.ContextMenu.Items
                        .OfType<MenuItem>()
                        .Should().ContainSingle(item => Equals(item.Header, expectedLabel));
                }
            }
            finally
            {
                window.Close();
            }
        }
    }
}
