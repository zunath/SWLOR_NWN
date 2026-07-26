using System.Text;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Logging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Doors;
using SWLOR.Toolset.Domain.Editors.Sounds;
using SWLOR.Toolset.Domain.Editors.Triggers;
using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.Doors;
using SWLOR.Toolset.Editors.Sounds;
using SWLOR.Toolset.Editors.Triggers;
using SWLOR.Toolset.Editors.Waypoints;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Proves the shared behavior-editor controls actually draw.
    /// </summary>
    /// <remarks>
    /// The rail and the header bind through <c>$parent[T].Property</c> rather than through the data
    /// context, which the compiler cannot check the way it checks a data-context path. Rendering
    /// each editor headlessly with the binding logger on is what turns a broken ancestor lookup into
    /// a failing test rather than an empty pane.
    /// </remarks>
    [NonParallelizable]
    public class BehaviorEditorViewRenderTests
    {
        [AvaloniaTest]
        public void TheWaypointEditorDrawsItsRailRowsAndHeader()
        {
            var editor = new WaypointEditorViewModel(
                Struct("UTW "),
                "wp_test",
                isInstance: false,
                Accept,
                new WaypointBehaviorCatalog(gameCode: null, transitionDestinationTags: null));

            AssertRenders(new WaypointDocumentView { DataContext = null }, editor.BehaviorList);
        }

        [AvaloniaTest]
        public void EveryBehaviorEditorRendersWithoutBindingErrors()
        {
            var previous = Logger.Sink;
            var sink = new CountingSink();
            Logger.Sink = sink;

            try
            {
                foreach (var view in BuildViews())
                {
                    var window = new Window { Width = 1200, Height = 800, Content = view };
                    window.Show();
                    Dispatcher.UIThread.RunJobs();

                    view.GetVisualDescendants().Should().NotBeEmpty(
                        $"{view.GetType().Name} must draw something");

                    window.Close();
                    Dispatcher.UIThread.RunJobs();
                }
            }
            finally
            {
                Logger.Sink = previous;
            }

            sink.Errors.Should().BeEmpty();
        }

        [AvaloniaTest]
        public void TheSharedRowDrawsEveryFieldKind()
        {
            var store = new BehaviorValueStore(Struct("UTW "));
            var rows = new[]
            {
                Row(store, "Tag", BehaviorFieldKind.Text, Domain.Gff.GffFieldType.CExoString),
                Row(store, "HasMapNote", BehaviorFieldKind.Check, Domain.Gff.GffFieldType.Byte),
                Row(store, "Appearance", BehaviorFieldKind.Integer, Domain.Gff.GffFieldType.Byte)
            };

            foreach (var row in rows)
            {
                var view = new BehaviorRowView { DataContext = row };
                var window = new Window { Width = 800, Height = 400, Content = view };
                window.Show();
                Dispatcher.UIThread.RunJobs();

                view.GetVisualDescendants().Should().NotBeEmpty();

                window.Close();
                Dispatcher.UIThread.RunJobs();
            }
        }

        private static IEnumerable<Control> BuildViews()
        {
            var waypoint = new WaypointEditorViewModel(
                Struct("UTW "),
                "wp_test",
                isInstance: false,
                Accept,
                new WaypointBehaviorCatalog(gameCode: null, transitionDestinationTags: null));
            yield return new BehaviorRailView
            {
                Items = waypoint.BehaviorList,
                ChooseCommand = waypoint.ChooseBehaviorCommand
            };

            var trigger = new TriggerEditorViewModel(
                Struct("UTT "), "trg_test", isInstance: false, Accept);
            yield return new BehaviorRailView
            {
                Items = trigger.BehaviorList,
                ChooseCommand = trigger.ChooseBehaviorCommand
            };

            var door = new DoorEditorViewModel(
                Struct("UTD "), "dor_test", isInstance: false, Accept);
            yield return new DoorEditorView { DataContext = door };

            var sound = new SoundEditorViewModel(
                Struct("UTS "), "snd_test", isInstance: false, Accept);
            yield return new SoundEditorView { DataContext = sound };

            yield return new BehaviorEditorHeader
            {
                BehaviorName = "Custom",
                Kind = "blueprint",
                Owner = "wp_test",
                IsDirty = true
            };
        }

        private static void AssertRenders(Control view, object items)
        {
            items.Should().NotBeNull();

            var window = new Window { Width = 1000, Height = 700, Content = view };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            view.GetVisualDescendants().Should().NotBeEmpty();

            window.Close();
            Dispatcher.UIThread.RunJobs();
        }

        private static WaypointRowViewModel Row(
            BehaviorValueStore store,
            string name,
            BehaviorFieldKind kind,
            Domain.Gff.GffFieldType type) =>
            new(
                new BehaviorFieldDefinition
                {
                    Label = name, Name = name, Kind = kind, FieldType = type
                },
                store,
                Accept);

        private static bool Accept(string description, Action mutation)
        {
            mutation();
            return true;
        }

        private static JsonGffStruct Struct(string dataType) =>
            JsonGffDocument.Parse(Encoding.UTF8.GetBytes($$"""
            {
              "__data_type": "{{dataType}}",
              "Tag": { "type": "cexostring", "value": "sample" }
            }
            """)).Root;

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
