using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Doors;
using SWLOR.Toolset.Domain.Editors.Merchants;
using SWLOR.Toolset.Domain.Editors.Sounds;
using SWLOR.Toolset.Domain.Editors.Triggers;
using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.Doors;
using SWLOR.Toolset.Editors.Merchants;
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
                new WaypointBehaviorCatalog(gameCodeIndex: null, transitionDestinationTags: null));

            AssertRenders(new WaypointEditorView { DataContext = editor }, editor.BehaviorList);
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
        public void MerchantEditorKeepsItsSelectedSectionWhenTheViewIsRecreated()
        {
            using var editor = new MerchantEditorViewModel(
                Struct("UTM "),
                "store_test",
                Accept,
                key => key == MerchantChoiceKeys.PaletteCategories
                    ? new[] { new BehaviorChoice(5, "Merchants") }
                    : Array.Empty<BehaviorChoice>());
            var window = new Window { Width = 1200, Height = 800 };

            try
            {
                var firstView = new MerchantEditorView { DataContext = editor };
                window.Content = firstView;
                window.Show();
                Dispatcher.UIThread.RunJobs();

                var firstTabs = firstView.FindControl<TabControl>("MerchantTabs")!;
                firstTabs.SelectedIndex = 1;
                Dispatcher.UIThread.RunJobs();
                editor.SelectedTabIndex.Should().Be(1);

                window.Content = null;
                Dispatcher.UIThread.RunJobs();
                var recreatedView = new MerchantEditorView { DataContext = editor };
                window.Content = recreatedView;
                Dispatcher.UIThread.RunJobs();

                recreatedView.FindControl<TabControl>("MerchantTabs")!
                    .SelectedIndex.Should().Be(1);
            }
            finally
            {
                window.Close();
                Dispatcher.UIThread.RunJobs();
            }
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

        [AvaloniaTest]
        public void BothPictureRowShapesDrawWithoutBindingErrors()
        {
            var previous = Logger.Sink;
            var sink = new CountingSink();
            Logger.Sink = sink;

            try
            {
                // The grid on the page and the grid behind the picture are different markup over the
                // same row, and the popup binds through an ancestor lookup the compiler cannot check.
                foreach (var count in new[] { 12, 400 })
                {
                    var row = PictureRow(count);
                    var view = new BehaviorRowView { DataContext = row };
                    var window = new Window { Width = 900, Height = 700, Content = view };
                    window.Show();
                    Dispatcher.UIThread.RunJobs();

                    row.OpenGalleryCommand.Execute(null);
                    Dispatcher.UIThread.RunJobs();

                    view.GetVisualDescendants().Should().NotBeEmpty();

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
        public void LoadedPopupArtworkDoesNotDrawItsFallbackTextBehindTransparentPixels()
        {
            var row = PictureRow(400);
            row.Choice = row.Choices[0];
            using var preview = new WriteableBitmap(
                new PixelSize(1, 1),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul);
            row.SelectedPreview = preview;

            var view = new BehaviorRowView { DataContext = row };
            var window = new Window { Width = 900, Height = 700, Content = view };

            try
            {
                window.Show();
                Dispatcher.UIThread.RunJobs();

                var selectedName = view.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Where(block => block.Text == row.SelectedChoiceDisplay)
                    .ToList();

                selectedName.Should().HaveCount(2,
                    "the popup picker has one fallback inside the preview and one caption below it");
                selectedName.Should().ContainSingle(block => block.IsVisible,
                    "the fallback must disappear once artwork is loaded so transparent pixels cannot reveal text");
            }
            finally
            {
                window.Close();
                Dispatcher.UIThread.RunJobs();
            }
        }

        [AvaloniaTest]
        public void SearchableRowsRealizeOnlyTheVisiblePartOfTheirFirstPage()
        {
            var previous = Logger.Sink;
            var sink = new CountingSink();
            Logger.Sink = sink;
            var row = new WaypointRowViewModel(
                new BehaviorFieldDefinition
                {
                    Label = "Dialog", Name = "Conversation", Kind = BehaviorFieldKind.Choice,
                    FieldType = Domain.Gff.GffFieldType.ResRef, IsSearchable = true
                },
                new BehaviorValueStore(Struct("UTW ")),
                Accept,
                Enumerable.Range(0, 500)
                    .Select(index => new BehaviorChoice($"dlg_{index}", $"Dialog {index}"))
                    .ToList());
            row.OpenSearchCommand.Execute(null);
            var view = new BehaviorRowView { DataContext = row };
            var window = new Window { Width = 800, Height = 400, Content = view };

            try
            {
                window.Show();
                Dispatcher.UIThread.RunJobs();

                row.FilteredChoices.Should().HaveCount(BehaviorRowViewModel.SearchPageSize);
                view.GetVisualDescendants().OfType<ListBoxItem>().Should().HaveCountLessThan(
                    row.FilteredChoices.Count,
                    "the list must virtualize its page instead of constructing every result control");
            }
            finally
            {
                window.Close();
                Dispatcher.UIThread.RunJobs();
                Logger.Sink = previous;
            }

            sink.Errors.Should().BeEmpty();
        }

        private static WaypointRowViewModel PictureRow(int count) =>
            new(
                new BehaviorFieldDefinition
                {
                    Label = "Appearance",
                    Name = "Appearance",
                    Kind = BehaviorFieldKind.Choice,
                    FieldType = Domain.Gff.GffFieldType.Byte
                },
                new BehaviorValueStore(Struct("UTW ")),
                Accept,
                Enumerable.Range(1, count)
                    .Select(id => new BehaviorChoice(
                        id, $"marker {id}", modelResRef: $"gi_waypoint{id:00}")
                    {
                        GalleryFacets =
                        [
                            new BehaviorChoiceFacet(
                                "group", "Group", id % 2 == 0 ? "even" : "odd",
                                id % 2 == 0 ? "Even" : "Odd")
                        ]
                    })
                    .ToList());

        private static IEnumerable<Control> BuildViews()
        {
            var waypoint = new WaypointEditorViewModel(
                Struct("UTW "),
                "wp_test",
                isInstance: false,
                Accept,
                new WaypointBehaviorCatalog(gameCodeIndex: null, transitionDestinationTags: null));
            yield return new BehaviorRailView
            {
                Items = waypoint.BehaviorList,
                ChooseCommand = waypoint.ChooseBehaviorCommand
            };
            yield return new WaypointEditorView { DataContext = waypoint };

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

            var merchant = new MerchantEditorViewModel(
                Struct("UTM "),
                "store_test",
                Accept,
                key => key == MerchantChoiceKeys.PaletteCategories
                    ? new[] { new BehaviorChoice(5, "Merchants") }
                    : Array.Empty<BehaviorChoice>());
            yield return new MerchantEditorView { DataContext = merchant };

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
