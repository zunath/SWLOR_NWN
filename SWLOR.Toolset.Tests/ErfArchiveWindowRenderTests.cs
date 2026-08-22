using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Archives;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public sealed class ErfArchiveWindowRenderTests
    {
        [AvaloniaTest]
        public void TheArchiveWorkflowLoadsItsCompiledXaml()
        {
            var settingsPath = Path.Combine(
                Path.GetTempPath(), $"swlor-erf-window-{Guid.NewGuid():N}.json");
            var settings = ToolsetSettings.Load(settingsPath);
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(path => new ModuleWorkspace(path), log);
            var viewModel = new ErfArchiveViewModel(
                new ErfArchiveService(workspace, log),
                settings);

            var window = new ErfArchiveWindow(viewModel);
            try
            {
                window.Show();
                Dispatcher.UIThread.RunJobs();

                window.DataContext.Should().BeSameAs(viewModel);
                window.Width.Should().Be(1180);
                window.Content.Should().NotBeNull();
                window.GetVisualDescendants().Should().NotBeEmpty();

                viewModel.Mode = ErfArchiveMode.Export;
                var selectableAsset = new ErfAssetRow(new ModuleArchiveAsset(
                    "render_test.nss",
                    "render_test",
                    "nss",
                    Path.Combine(Path.GetTempPath(), "render_test.nss"),
                    1,
                    "Script source"));
                viewModel.Assets.Add(selectableAsset);
                viewModel.SearchText = "render_test";
                viewModel.CurrentStep = 1;
                Dispatcher.UIThread.RunJobs();

                var headerCheckbox = window.GetVisualDescendants()
                    .OfType<CheckBox>()
                    .SingleOrDefault(checkBox =>
                        checkBox.Name == "VisibleSelectionCheckBox");
                headerCheckbox.Should().NotBeNull();
                headerCheckbox!.IsVisible.Should().BeTrue();
                headerCheckbox.IsEnabled.Should().BeTrue();
                var assetTableHost = window.FindControl<Grid>("AssetTableHost");
                assetTableHost.Should().NotBeNull();
                var headerPosition = headerCheckbox.TranslatePoint(
                    new Point(0, 0),
                    assetTableHost);
                headerPosition.Should().NotBeNull();
                headerPosition!.Value.X.Should().BeApproximately(
                    (42 - headerCheckbox.Bounds.Width) / 2,
                    0.5);
                headerPosition.Value.Y.Should().BeApproximately(
                    (42 - headerCheckbox.Bounds.Height) / 2,
                    0.5);
                headerCheckbox.Command.Should().NotBeNull();
                headerCheckbox.Command!.Execute(headerCheckbox.CommandParameter);
                Dispatcher.UIThread.RunJobs();
                selectableAsset.IsSelected.Should().BeTrue();
                headerCheckbox.IsChecked.Should().BeTrue();
                var rowCheckbox = window.GetVisualDescendants()
                    .OfType<CheckBox>()
                    .Single(checkBox => checkBox != headerCheckbox);
                headerCheckbox.Bounds.Width.Should().Be(rowCheckbox.Bounds.Width);
                headerCheckbox.Bounds.Height.Should().Be(rowCheckbox.Bounds.Height);
                window.GetVisualDescendants()
                    .OfType<Button>()
                    .Select(button => button.Content?.ToString())
                    .Should().NotContain(new[] { "Select shown", "Clear shown" });

                viewModel.CurrentStep = 2;
                viewModel.IsValidatingSelection = true;
                viewModel.IsBusy = true;
                Dispatcher.UIThread.RunJobs();

                var validationProgress =
                    window.FindControl<Border>("SelectionValidationProgress");
                validationProgress.Should().NotBeNull();
                validationProgress!.IsVisible.Should().BeTrue();
                viewModel.StepTitle.Should().Be("Validating selected assets");
                viewModel.ShowExportValidation.Should().BeFalse();

                var closeButton = window.GetVisualDescendants()
                    .OfType<Button>()
                    .Single(button => button.Content?.ToString() == "Close");
                closeButton.IsEnabled.Should().BeFalse();
                window.Close();
                Dispatcher.UIThread.RunJobs();
                window.IsVisible.Should().BeTrue(
                    "window chrome must not dispose an archive operation that is still active");

                viewModel.IsBusy = false;
                Dispatcher.UIThread.RunJobs();
                closeButton.IsEnabled.Should().BeTrue();

                viewModel.Mode = ErfArchiveMode.Import;
                viewModel.CurrentStep = 3;
                viewModel.IsComplete = true;
                Dispatcher.UIThread.RunJobs();

                var importAgainButton = window.GetVisualDescendants()
                    .OfType<Button>()
                    .Single(button => button.Content?.ToString() == "Import another ERF");
                importAgainButton.IsVisible.Should().BeTrue();
                importAgainButton.IsEnabled.Should().BeTrue();
                window.GetVisualDescendants()
                    .OfType<Button>()
                    .Single(button => button.Content?.ToString() == "Import to Module")
                    .IsVisible.Should().BeFalse();
            }
            finally
            {
                viewModel.IsBusy = false;
                window.Close();
                Dispatcher.UIThread.RunJobs();
            }
        }
    }
}
