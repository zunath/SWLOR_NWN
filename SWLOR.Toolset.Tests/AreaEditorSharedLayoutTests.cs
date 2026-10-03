using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Nwn.Toolset.Avalonia.Areas;
using NUnit.Framework;
using SWLOR.Toolset.Editors;

namespace SWLOR.Toolset.Tests;

[NonParallelizable]
public sealed class AreaEditorSharedLayoutTests
{
    [AvaloniaTest]
    public void ExistingAreaViewUsesSharedTabsWithoutReplacingItsSceneOrProperties()
    {
        var view = new AreaEditorView();
        var window = new Window { Width = 1200, Height = 850, Content = view };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            var layout = view.FindControl<AreaEditorLayout>("RootTabs")!;
            var surface = view.FindControl<AreaEditorSurface>("AreaView")!;
            var properties = view.FindControl<ScrollViewer>("PropertiesScroll")!;
            layout.Scene!.GetVisualDescendants().Should().Contain(surface);
            surface.Bounds.Height.Should().BeGreaterThan(650);
            layout.SelectedIndex = 1;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            layout.Properties.Should().BeSameAs(properties);
            layout.GetVisualDescendants().Should().Contain(properties);
            layout.SelectedIndex = 0;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            surface.Bounds.Height.Should().BeGreaterThan(650);
        }
        finally
        {
            window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }
}
