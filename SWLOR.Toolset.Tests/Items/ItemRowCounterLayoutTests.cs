using System.Text;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// The character counter beside Name/Tag/ResRef must show its whole "used/max" text at the
    /// widths the item editor's Basic pane actually runs at.
    /// </summary>
    [NonParallelizable]
    public class ItemRowCounterLayoutTests
    {
        [AvaloniaTest]
        public void TheCounterShowsUsedAndMaximum()
        {
            // The real editor view at the pane width of a 1080p session, so the whole container
            // stack - tab, scroll gutter, item margins - is the one the counter actually sits in.
            var editor = new SWLOR.Toolset.Editors.Items.ItemEditorViewModel(
                Item(), "keyanzioneclothi", (_, mutation) => { mutation(); return true; });
            var view = new SWLOR.Toolset.Editors.Items.ItemEditorView { DataContext = editor };
            var window = new Window { Width = 1010, Height = 700, Content = view };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var counters = view.GetVisualDescendants()
                .OfType<TextBlock>()
                .Where(block => (block.Text ?? string.Empty).Contains('/'))
                .ToList();

            counters.Should().NotBeEmpty("Name/Tag/ResRef all carry a length counter");
            foreach (var counter in counters)
            {
                TestContext.Out.WriteLine(
                    $"counter text='{counter.Text}' bounds={counter.Bounds.Width:0.#} " +
                    $"desired={counter.DesiredSize.Width:0.#}");
            }

            counters.Select(counter => counter.Text)
                .Should().Contain("18/64", "the name is 18 of its 64 characters");
            foreach (var counter in counters)
            {
                counter.Bounds.Width.Should().BeGreaterThanOrEqualTo(
                    counter.DesiredSize.Width - 0.5,
                    $"'{counter.Text}' clipped to its first characters reads as a broken box");
            }

            window.Close();
            Dispatcher.UIThread.RunJobs();
            GC.KeepAlive(editor);
        }

        /// <summary>
        /// A small window must not squeeze the fields between the preview rail and the Flags card
        /// until their content renders as ellipses - the rails shrink and the card reflows below
        /// instead.
        /// </summary>
        [AvaloniaTest]
        public void ASmallWindowGivesTheFieldsTheSpaceTheFixedCompanionsWereHolding()
        {
            var editor = new SWLOR.Toolset.Editors.Items.ItemEditorViewModel(
                Item(), "keyanzioneclothi", (_, mutation) => { mutation(); return true; });
            var view = new SWLOR.Toolset.Editors.Items.ItemEditorView { DataContext = editor };
            var window = new Window { Width = 1400, Height = 700, Content = view };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var rail = view.GetVisualDescendants().OfType<Border>()
                .Single(border => border.Name == "PreviewRail");
            var flags = view.GetVisualDescendants().OfType<Border>()
                .Single(border => border.Name == "FlagsCard");

            rail.IsVisible.Should().BeTrue("a wide window has room for everything");
            Grid.GetColumn(flags).Should().Be(1, "the Flags card sits beside the fields when it fits");

            // A laptop-sized pane: the card reflows under the fields and the rails give up width.
            window.Width = 820;
            Dispatcher.UIThread.RunJobs();

            Grid.GetColumn(flags).Should().Be(0, "the card moves out of the fields' column");
            Grid.GetRow(flags).Should().Be(1, "and onto its own row beneath them");
            rail.Bounds.Width.Should().BeLessThan(190, "the preview rail gives up width first");
            FieldsWidth(view).Should().BeGreaterThan((820 - rail.Bounds.Width) * 0.8,
                "the fields take what is left of the pane rather than sharing it with the card");

            // Smaller still: the preview rail is the last thing left to give.
            window.Width = 620;
            Dispatcher.UIThread.RunJobs();

            rail.IsVisible.Should().BeFalse();
            FieldsWidth(view).Should().BeGreaterThan(620 * 0.85,
                "with nothing fixed left beside them, the fields own the whole pane");

            window.Close();
            Dispatcher.UIThread.RunJobs();
            GC.KeepAlive(editor);
        }

        /// <summary>The rendered width of the Basic tab's field column.</summary>
        private static double FieldsWidth(Control view) =>
            view.GetVisualDescendants().OfType<ItemsControl>()
                .Where(items => items.Bounds.Width > 0)
                .Select(items => items.Bounds.Width)
                .DefaultIfEmpty(0)
                .Max();

        private static JsonGffStruct Item() =>
            JsonGffDocument.Parse(Encoding.UTF8.GetBytes("""
            {
              "__data_type": "UTI ",
              "LocalizedName": { "type": "cexolocstring", "value": { "0": "(NPC) Keyan's Robe" } },
              "Tag": { "type": "cexostring", "value": "KeyanZioneClothing" }
            }
            """)).Root;
    }
}
