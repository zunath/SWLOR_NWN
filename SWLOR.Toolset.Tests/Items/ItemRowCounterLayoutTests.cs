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
