using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
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
        public async Task StartupFailureCanReturnToWorkspaceSelection()
        {
            var window = new MainWindow((ToolsetSettings?)null);
            var recovered = false;
            window.ShowStartupError("Missing workspace assets.", () => { recovered = true; return Task.CompletedTask; });
            var button = window.FindControl<Button>("StartupWorkspaceButton")!;
            button.IsVisible.Should().BeTrue();
            button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            await Task.Yield();
            recovered.Should().BeTrue();
        }

        [AvaloniaTest]
        public async Task FailedWorkspaceRecoveryRemainsRetryable()
        {
            var window = new MainWindow((ToolsetSettings?)null);
            window.ShowStartupError("Missing assets.", () => Task.FromException(new IOException("Unavailable folder.")));
            var button = window.FindControl<Button>("StartupWorkspaceButton")!;
            button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            await Task.Yield();
            window.FindControl<TextBlock>("StartupStatus")!.Text.Should().Contain("Unavailable folder.");
            button.IsEnabled.Should().BeTrue();
            button.IsVisible.Should().BeTrue();
        }

        [AvaloniaTest]
        public void FailedAttachedShellCanBeResetAndAttachedAgain()
        {
            var settings = ToolsetSettings.Load(Path.Combine(Path.GetTempPath(), "swlor_reset_" + Guid.NewGuid().ToString("N") + ".json"));
            var services = new ServiceCollection();
            typeof(App).GetMethod("ConfigureServices", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [services, settings]);
            using var provider = services.BuildServiceProvider();
            var shell = provider.GetRequiredService<ShellViewModel>();
            var window = new MainWindow(settings);
            try
            {
                window.AttachViewModel(shell);
                window.KeyBindings.Should().NotBeEmpty();
                window.ResetAfterStartupFailure();
                window.DataContext.Should().BeNull();
                window.KeyBindings.Should().BeEmpty();
                Action retry = () => window.AttachViewModel(shell);
                retry.Should().NotThrow();
            }
            finally { window.ResetAfterStartupFailure(); }
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
