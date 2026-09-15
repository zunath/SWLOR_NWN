using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Shell;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Guards the lightweight surface that appears before the interactive shell finishes composing.
    /// </summary>
    public class ToolsetStartupViewTests
    {
        [AvaloniaTest]
        public void MainWindowCanRenderBeforeTheShellExists()
        {
            var settingsPath = Path.Combine(
                Path.GetTempPath(), $"swlor-toolset-startup-{Guid.NewGuid():N}.json");
            var window = new MainWindow(ToolsetSettings.Load(settingsPath));

            window.DataContext.Should().BeNull(
                "the first paint must not wait for the game-data-backed shell");
            window.FindControl<Border>("StartupPanel")!.IsVisible.Should().BeTrue();
            window.FindControl<ProgressBar>("StartupProgress")!.IsIndeterminate.Should().BeTrue();
            window.FindControl<TextBlock>("StartupStatus")!.Text.Should().Contain("loading in the background");
        }

        [AvaloniaTest]
        public void LongSetupInstructionsRemainScrollableInASmallWindow()
        {
            var window = new MainWindow((ToolsetSettings?)null) { Width = 640, Height = 400 };
            window.ShowStartupError(string.Join("\n", Enumerable.Repeat("Missing HAK content", 40)));
            window.Show();
            try
            {
                window.UpdateLayout();
                var scroll = window.FindControl<ScrollViewer>("StartupScroll")!;
                scroll.Extent.Height.Should().BeGreaterThan(scroll.Viewport.Height);
                scroll.Offset = new Avalonia.Vector(0, scroll.Extent.Height);
                window.UpdateLayout();
                scroll.Offset.Y.Should().BeGreaterThan(0);
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaTest]
        public void BootstrapFailureLeavesAnActionableVisibleState()
        {
            var settingsPath = Path.Combine(
                Path.GetTempPath(), $"swlor-toolset-startup-{Guid.NewGuid():N}.json");
            var window = new MainWindow(ToolsetSettings.Load(settingsPath));

            window.ShowStartupError("Could not load game data.");

            window.FindControl<Border>("StartupPanel")!.IsVisible.Should().BeTrue();
            window.FindControl<ProgressBar>("StartupProgress")!.IsIndeterminate.Should().BeFalse();
            window.FindControl<TextBlock>("StartupStatus")!.Text.Should().Be("Could not load game data.");
            window.FindControl<TextBlock>("StartupTitle")!.Text.Should().Be("Toolset setup needs attention");
            window.FindControl<SelectableTextBlock>("StartupStatus").Should().NotBeNull(
                "builders need to copy the asset setup command and repository path");
        }
    }
}
