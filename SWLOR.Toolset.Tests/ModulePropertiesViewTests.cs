using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Logging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Module;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Shell;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    public class ModulePropertiesViewTests
    {
        [AvaloniaTest]
        public void EditorUsesTheConsolidatedTabsAndFullWidthBasicFields()
        {
            var view = new ModulePropertiesDocumentView();
            var window = new Window { Width = 1200, Height = 800, Content = view };
            window.Show();

            view.GetVisualDescendants()
                .OfType<TabItem>()
                .Select(tab => tab.Header?.ToString())
                .Should()
                .Equal("Basic", "Events", "Variables", "Description", "Custom Content", "SWLOR");
            window.UpdateLayout();
            var moduleName = view.FindControl<TextBox>("ModuleNameField")!;
            var moduleTag = view.FindControl<TextBox>("ModuleTagField")!;
            moduleName.Bounds.Width.Should().BeGreaterThan(800);
            moduleTag.Bounds.Width.Should().BeApproximately(moduleName.Bounds.Width, 0.1);
            view.FindControl<TextBox>("EntryAreaField")!.IsReadOnly.Should().BeTrue();
            view.FindControl<TextBox>("EntryXField")!.IsReadOnly.Should().BeTrue();
            view.FindControl<TextBox>("EntryYField")!.IsReadOnly.Should().BeTrue();
            view.FindControl<TextBox>("EntryZField")!.IsReadOnly.Should().BeTrue();
            view.FindControl<TextBox>("EntryAreaField")!.Bounds.Width
                .Should().BeApproximately(moduleName.Bounds.Width, 0.1);
            view.FindControl<ComboBox>("StartingMoviePicker")!.HorizontalAlignment
                .Should().Be(Avalonia.Layout.HorizontalAlignment.Stretch);

            window.Close();
        }

        [AvaloniaTest]
        public void EditMenuContainsTheModulePropertiesEntryPoint()
        {
            var settingsPath = Path.Combine(Path.GetTempPath(), $"swlor-toolset-module-properties-{Guid.NewGuid():N}.json");
            var window = new MainWindow(ToolsetSettings.Load(settingsPath));

            window.FindControl<MenuItem>("ModulePropertiesMenuItem")!.Header
                .Should().Be("Module _Properties...");
        }

        [AvaloniaTest]
        public void EventScriptPickerStretchesAcrossItsDataGridCell()
        {
            var ifoPath = Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json");
            var editor = new ModulePropertiesDocumentViewModel(
                ifoPath,
                CorpusLocator.ModuleDirectory,
                new ModuleWorkspace(CorpusLocator.ModuleDirectory),
                new OutputLogService(),
                new StubPrompts());
            var view = new ModulePropertiesDocumentView { DataContext = editor };
            var window = new Window { Width = 1200, Height = 800, Content = view };

            window.Show();
            view.FindControl<TabControl>("ModulePropertyTabs")!.SelectedIndex = 1;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            var picker = view.GetVisualDescendants().OfType<ComboBox>().First();
            var cell = picker.GetVisualAncestors().OfType<DataGridCell>().Single();
            cell.HorizontalContentAlignment.Should().Be(Avalonia.Layout.HorizontalAlignment.Stretch);
            picker.Bounds.Width.Should().BeGreaterThan(cell.Bounds.Width - 12);
            picker.ItemsSource!.Cast<string>().Should().Contain("colors_inc");
            picker.ItemsSource!.Cast<string>().Should().Contain("nw_c2_default1");
            picker.SelectedItem.Should().Be(editor.Events[0].Script);

            window.Close();
            editor.OnClose().Should().BeTrue();
        }

        [AvaloniaTest]
        public void StartingMovieUsesTheDiscoveredMovieDropdown()
        {
            var ifoPath = Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json");
            var editor = new ModulePropertiesDocumentViewModel(
                ifoPath,
                CorpusLocator.ModuleDirectory,
                new ModuleWorkspace(CorpusLocator.ModuleDirectory),
                new OutputLogService(),
                new StubPrompts());
            var view = new ModulePropertiesDocumentView { DataContext = editor };
            var window = new Window { Width = 1200, Height = 800, Content = view };

            window.Show();
            view.FindControl<TabControl>("ModulePropertyTabs")!.SelectedIndex = 0;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            var picker = view.FindControl<ComboBox>("StartingMoviePicker");
            picker.Should().NotBeNull();
            picker!.ItemsSource.Should().BeSameAs(editor.StartingMovieChoices);
            editor.StartingMovieChoices.Should().Contain(editor.StartingMovie);

            window.Close();
            editor.OnClose().Should().BeTrue();
        }

        [AvaloniaTest]
        public void XpScaleSliderAndNumericInputStaySynchronized()
        {
            var ifoPath = Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json");
            var editor = new ModulePropertiesDocumentViewModel(
                ifoPath,
                CorpusLocator.ModuleDirectory,
                new ModuleWorkspace(CorpusLocator.ModuleDirectory),
                new OutputLogService(),
                new StubPrompts());
            var view = new ModulePropertiesDocumentView { DataContext = editor };
            var window = new Window { Width = 1200, Height = 800, Content = view };

            window.Show();
            view.FindControl<TabControl>("ModulePropertyTabs")!.SelectedIndex = 0;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            var slider = view.FindControl<Slider>("XpScaleSlider")!;
            var numeric = view.FindControl<NumericUpDown>("XpScaleNumeric")!;
            var startingYear = view.FindControl<NumericUpDown>("StartingYearNumeric")!;
            var numericInputs = view.GetVisualDescendants().OfType<NumericUpDown>().ToArray();
            numericInputs.Should().HaveCount(8);
            numericInputs.Should().OnlyContain(input => input.Bounds.Width <= 130);
            startingYear.Bounds.Width.Should().BeApproximately(130, 0.1);
            startingYear.GetVisualDescendants().OfType<TextBox>().Single().Bounds.Width
                .Should().BeGreaterThan(50);
            numeric.Bounds.Height.Should().BeApproximately(numericInputs[0].Bounds.Height, 0.1);
            slider.Minimum.Should().Be(0);
            slider.Maximum.Should().Be(200);
            slider.Value.Should().Be((double)editor.XpScale);
            numeric.Value.Should().Be(editor.XpScale);

            slider.Value = 37;
            Dispatcher.UIThread.RunJobs();
            editor.XpScale.Should().Be(37);
            numeric.Value.Should().Be(37);

            editor.Undo();
            numeric.Value = 81;
            Dispatcher.UIThread.RunJobs();
            editor.XpScale.Should().Be(81);
            slider.Value.Should().Be(81);

            editor.Undo();
            editor.IsDirty.Should().BeFalse();
            window.Close();
            editor.OnClose().Should().BeTrue();
        }

        [AvaloniaTest]
        public void HakPriorityControlsUseCompactArrowIcons()
        {
            var view = new ModulePropertiesDocumentView();
            var window = new Window { Width = 1200, Height = 800, Content = view };

            window.Show();
            view.FindControl<TabControl>("ModulePropertyTabs")!.SelectedIndex = 4;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            var moveUp = view.FindControl<Button>("MoveHakUpButton")!;
            var moveDown = view.FindControl<Button>("MoveHakDownButton")!;
            moveUp.Content.Should().BeOfType<Avalonia.Controls.Shapes.Path>();
            moveDown.Content.Should().BeOfType<Avalonia.Controls.Shapes.Path>();
            ToolTip.GetTip(moveUp).Should().Be("Move selected HAK up");
            ToolTip.GetTip(moveDown).Should().Be("Move selected HAK down");
            moveUp.Bounds.Width.Should().BeLessThan(30);
            moveDown.Bounds.Width.Should().BeLessThan(30);

            window.Close();
        }

        [AvaloniaTest]
        public void EveryModulePropertiesTabRendersWithoutBindingErrors()
        {
            var previousSink = Logger.Sink;
            var sink = new CountingSink();
            Logger.Sink = sink;
            var ifoPath = Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json");
            var editor = new ModulePropertiesDocumentViewModel(
                ifoPath,
                CorpusLocator.ModuleDirectory,
                new ModuleWorkspace(CorpusLocator.ModuleDirectory),
                new OutputLogService(),
                new StubPrompts());

            try
            {
                var view = new ModulePropertiesDocumentView { DataContext = editor };
                var window = new Window { Width = 1200, Height = 800, Content = view };
                window.Show();
                var tabs = view.FindControl<TabControl>("ModulePropertyTabs")!;

                for (var index = 0; index < 6; index++)
                {
                    tabs.SelectedIndex = index;
                    Dispatcher.UIThread.RunJobs();
                    window.UpdateLayout();
                    view.GetVisualDescendants().Should().NotBeEmpty();
                }

                window.Close();
                editor.OnClose().Should().BeTrue();
            }
            finally
            {
                Logger.Sink = previousSink;
            }

            sink.Errors.Should().BeEmpty();
        }

        private sealed class StubPrompts : IEditorPromptService
        {
            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel) =>
                Task.FromResult(false);

            public Task<string?> PromptForTextAsync(
                string headline,
                string message,
                string initialValue,
                string confirmLabel) => Task.FromResult<string?>(null);
        }

        private sealed class CountingSink : ILogSink
        {
            public List<string> Errors { get; } = new();

            public bool IsEnabled(LogEventLevel level, string area) =>
                level >= LogEventLevel.Warning && area == LogArea.Binding;

            public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
            {
                if (IsEnabled(level, area))
                    Errors.Add(messageTemplate);
            }

            public void Log(
                LogEventLevel level,
                string area,
                object? source,
                string messageTemplate,
                params object?[] values)
            {
                if (IsEnabled(level, area))
                    Errors.Add(messageTemplate + " | " + string.Join(", ", values));
            }
        }
    }
}
