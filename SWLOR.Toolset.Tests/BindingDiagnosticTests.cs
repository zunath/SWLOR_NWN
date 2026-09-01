using System.Reflection;
using Avalonia.Headless.NUnit;
using Avalonia.Logging;
using Avalonia.Threading;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Shell;
using SWLOR.Toolset.Shell.Panels;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Exercises the real first-paint and docked-shell visual trees with Avalonia's binding logger
    /// enabled. Compiled bindings catch missing members at build time; this catches valid paths that
    /// still fail at runtime because an intermediate model object is null.
    /// </summary>
    [NonParallelizable]
    public class BindingDiagnosticTests
    {
        [AvaloniaTest]
        public void StartupAndDockedShellDoNotLogBindingErrors()
        {
            var previousSink = Logger.Sink;
            var sink = new BindingLogSink();
            Logger.Sink = sink;

            var settingsPath = Path.Combine(
                Path.GetTempPath(), $"swlor-toolset-bindings-{Guid.NewGuid():N}.json");
            var settings = ToolsetSettings.Load(settingsPath);
            var window = new MainWindow(settings);
            ServiceProvider? provider = null;

            try
            {
                window.Show();
                Dispatcher.UIThread.RunJobs();

                var services = new ServiceCollection();
                typeof(App)
                    .GetMethod("ConfigureServices", BindingFlags.NonPublic | BindingFlags.Static)!
                    .Invoke(null, [services, settings]);
                provider = services.BuildServiceProvider();

                var shell = provider.GetRequiredService<ShellViewModel>();
                window.AttachViewModel(shell);
                Dispatcher.UIThread.RunJobs();

                var factory = provider.GetRequiredService<ToolsetDockFactory>();
                var factoryCreatedDockables = new IDockable[]
                {
                    factory.CreateRootDock(),
                    factory.CreateProportionalDock(),
                    factory.CreateDockDock(),
                    factory.CreateStackDock(),
                    factory.CreateGridDock(),
                    factory.CreateWrapDock(),
                    factory.CreateUniformGridDock(),
                    factory.CreateProportionalDockSplitter(),
                    factory.CreateGridDockSplitter(),
                    factory.CreateToolDock(),
                    factory.CreateDocumentDock(),
                    factory.CreateSplitViewDock(),
                    factory.CreateDocument(),
                    factory.CreateTool()
                };
                Assert.Multiple(() =>
                {
                    foreach (var dockable in factoryCreatedDockables)
                    {
                        Assert.That(dockable.DockCapabilityOverrides, Is.Not.Null);
                        if (dockable is IDock dock)
                            Assert.That(dock.DockCapabilityPolicy, Is.Not.Null);
                        if (dockable is IRootDock root)
                            Assert.That(root.RootDockCapabilityPolicy, Is.Not.Null);
                    }
                });

                var dockedTools = new IDockable[]
                {
                    provider.GetRequiredService<ModuleExplorerViewModel>(),
                    provider.GetRequiredService<PaletteViewModel>(),
                    provider.GetRequiredService<ScriptReferenceViewModel>(),
                    provider.GetRequiredService<OutputViewModel>(),
                    provider.GetRequiredService<ValidationViewModel>(),
                    provider.GetRequiredService<ProblemsViewModel>()
                };
                foreach (var tool in dockedTools)
                {
                    factory.Focus(tool);
                    Dispatcher.UIThread.RunJobs();
                }

                factory.OpenDocument(new BindingAuditViewModel
                {
                    Id = "binding-audit-document",
                    Title = "Binding audit"
                });
                Dispatcher.UIThread.RunJobs();

                Assert.That(
                    sink.Errors,
                    Is.Empty,
                    "The startup surface, every initially docked tool, and a document tab should have valid " +
                    "runtime bindings:" + Environment.NewLine + string.Join(Environment.NewLine, sink.Errors));
            }
            finally
            {
                window.Hide();
                provider?.Dispose();
                Logger.Sink = previousSink;
            }
        }

        [AvaloniaTest]
        public async Task ActiveResourceDeletionBlocksApplicationClose()
        {
            var settingsPath = Path.Combine(
                Path.GetTempPath(), $"swlor-toolset-close-{Guid.NewGuid():N}.json");
            var settings = ToolsetSettings.Load(settingsPath);
            var services = new ServiceCollection();
            typeof(App)
                .GetMethod("ConfigureServices", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [services, settings]);
            var previousAmbient = ModuleMutationLock.ModuleWrites;
            using var provider = services.BuildServiceProvider();

            try
            {
                var shell = provider.GetRequiredService<ShellViewModel>();
                var mutationLock = provider.GetRequiredService<ModuleMutationLock>();

                using (mutationLock.BeginResourceDeletion())
                {
                    shell.IsModuleMutationLocked.Should().BeTrue();
                    (await shell.TryCloseAsync()).Should().BeFalse(
                        "closing would terminate the background delete before its UI cleanup finishes");
                    shell.StatusText.Should().Contain("active module operation");
                }

                shell.IsModuleMutationLocked.Should().BeFalse();
            }
            finally
            {
                ModuleMutationLock.ModuleWrites = previousAmbient;
                if (File.Exists(settingsPath))
                    File.Delete(settingsPath);
            }
        }

        private sealed class BindingLogSink : ILogSink
        {
            public List<string> Errors { get; } = [];

            public bool IsEnabled(LogEventLevel level, string area) =>
                area == LogArea.Binding && level >= LogEventLevel.Warning;

            public void Log(
                LogEventLevel level,
                string area,
                object? source,
                string messageTemplate)
            {
                Errors.Add(messageTemplate);
            }

            public void Log(
                LogEventLevel level,
                string area,
                object? source,
                string messageTemplate,
                params object?[] propertyValues)
            {
                Errors.Add(
                    $"{messageTemplate} :: {string.Join(", ", propertyValues.Select(value => value ?? "<null>"))}");
            }
        }
    }

    public sealed class BindingAuditViewModel : Document
    {
    }

    public sealed class BindingAuditView : Avalonia.Controls.Control
    {
    }
}
