using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using FluentAssertions;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Areas.Contents;
using Nwn.Toolset.Avalonia.Areas.Contents.Views;
using NUnit.Framework;

namespace SWLOR.Toolset.Tests;

/// <summary>Exercises the real pointer-to-popup path of the shared contents tree.</summary>
public sealed class AreaContentsContextMenuTests
{
    [AvaloniaTest]
    public void RightClickingAnInstanceRowOpensItsPropertiesMenu() =>
        RightClickingARowMatchesItsPropertiesMenu(AreaContentsNodeKind.Instance, "Open properties...");

    [AvaloniaTest]
    public void RightClickingAGroupRowOpensItsFirstInstancePropertiesMenu() =>
        RightClickingARowMatchesItsPropertiesMenu(AreaContentsNodeKind.Group, "Open first instance properties...");

    [AvaloniaTest]
    public void RightClickingAKindHeadingDoesNotOpenAStalePropertiesMenu() =>
        RightClickingARowMatchesItsPropertiesMenu(AreaContentsNodeKind.Kind, null);

    private static void RightClickingARowMatchesItsPropertiesMenu(AreaContentsNodeKind kind, string? expectedLabel)
    {
        var row = new AreaContentsNodeViewModel(kind, ModuleResourceType.Utc, "Test creature", 1,
            "Open properties...", "Open first instance properties...")
        {
            Identities = kind == AreaContentsNodeKind.Group
                ? new[] { new AreaContentsIdentity(ModuleResourceType.Utc, 0), new AreaContentsIdentity(ModuleResourceType.Utc, 1) }
                : kind == AreaContentsNodeKind.Instance ? new[] { new AreaContentsIdentity(ModuleResourceType.Utc, 0) } : Array.Empty<AreaContentsIdentity>()
        };
        var viewModel = new AreaContentsViewModel();
        viewModel.Rows.Add(row);
        var view = new AreaContentsView { Contents = viewModel };
        var window = new Window { Content = view, Width = 400, Height = 300 };
        window.Show();
        try
        {
            var rowSurface = view.GetVisualDescendants().OfType<Grid>()
                .Single(control => ReferenceEquals(control.DataContext, row) && control.ContextMenu is not null);
            var rowContainer = rowSurface.FindAncestorOfType<ListBoxItem>()!;
            var point = rowContainer.TranslatePoint(new Point(rowContainer.Bounds.Width - 2, rowContainer.Bounds.Height / 2), window)!.Value;
            window.MouseMove(point, RawInputModifiers.None);
            window.MouseDown(point, MouseButton.Right, RawInputModifiers.RightMouseButton);
            window.MouseUp(point, MouseButton.Right, RawInputModifiers.None);
            rowSurface.ContextMenu!.IsOpen.Should().Be(expectedLabel is not null);
            if (expectedLabel is not null)
                rowSurface.ContextMenu.Items.OfType<MenuItem>().Should().ContainSingle(item => Equals(item.Header, expectedLabel));
        }
        finally { window.Close(); }
    }
}
