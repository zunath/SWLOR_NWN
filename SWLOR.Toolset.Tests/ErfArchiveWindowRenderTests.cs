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
            }
            finally
            {
                window.Close();
                Dispatcher.UIThread.RunJobs();
            }
        }
    }
}
