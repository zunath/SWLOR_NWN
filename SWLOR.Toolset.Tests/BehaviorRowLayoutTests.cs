using System.Text;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Doors;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.Doors;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// What a behavior row actually gives its value control at the widths a builder works at.
    /// </summary>
    /// <remarks>
    /// Measured rather than eyeballed. Every previous attempt at this was a guess from a screenshot,
    /// and the numbers that matter - how much of the row the label takes, and how much reaches the
    /// control - are only knowable by laying the row out.
    /// </remarks>
    [NonParallelizable]
    public class BehaviorRowLayoutTests
    {
        /// <summary>The editor pane on a 1080p screen with both side panels open.</summary>
        private const double NarrowPane = 460;

        [AvaloniaTest]
        public void TheValueControlGetsMostOfANarrowRow()
        {
            var (labelWidth, valueWidth) = Measure(NarrowPane);

            TestContext.Out.WriteLine($"label={labelWidth:0.#} value={valueWidth:0.#}");

            valueWidth.Should().BeGreaterThan(labelWidth,
                "the value is the part that has to hold a tag, a search list or a picture grid");
            valueWidth.Should().BeGreaterThan(NarrowPane * 0.5,
                "a row that spends half its width before the control starts is unusable when the "
                + "pane is narrow");
        }

        [AvaloniaTest]
        public void ANarrowPaneStacksTheLabelInsteadOfClippingTheControl()
        {
            // The width the placeable behavior page leaves a field once the 3D preview and the
            // behavior rail have taken theirs, on a window that is not itself unreasonable.
            const double cramped = 300;

            var (row, view, window) = Build(cramped);
            var panel = view.GetVisualDescendants().OfType<LabeledFieldPanel>().First();
            var box = view.GetVisualDescendants().OfType<TextBox>().First();

            TestContext.Out.WriteLine(
                $"stacked={panel.IsStacked} panel={panel.Bounds.Width:0.#} box={box.Bounds.Width:0.#}");

            panel.IsStacked.Should().BeTrue(
                "below the threshold the label moves above the control rather than squeezing it");
            box.Bounds.Width.Should().BeGreaterThan(cramped * 0.7,
                "a stacked row hands the whole width to the control");
            panel.Bounds.Width.Should().BeLessThanOrEqualTo(cramped,
                "the row must fit the pane rather than overflow it and be clipped");

            window.Close();
            Dispatcher.UIThread.RunJobs();
            GC.KeepAlive(row);
        }

        private static (double Label, double Value) Measure(double width)
        {
            var (_, view, window) = Build(width);

            var label = view.GetVisualDescendants()
                .OfType<TextBlock>()
                .First(block => block.Text == "Opens with key");
            var box = view.GetVisualDescendants().OfType<TextBox>().First();

            var result = (label.Bounds.Width, box.Bounds.Width);
            window.Close();
            Dispatcher.UIThread.RunJobs();
            return result;
        }

        private static (DoorRowViewModel Row, BehaviorRowView View, Window Window) Build(double width)
        {
            var row = new DoorRowViewModel(
                new DoorFieldDefinition
                {
                    Label = "Opens with key",
                    Name = "KeyName",
                    Kind = BehaviorFieldKind.Text,
                    FieldType = GffFieldType.CExoString
                },
                new DoorValueStore(Door()),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                null,
                _ => { },
                _ => { });

            var view = new BehaviorRowView { DataContext = row };
            var window = new Window { Width = width, Height = 300, Content = view };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            return (row, view, window);
        }

        private static JsonGffStruct Door() =>
            JsonGffDocument.Parse(Encoding.UTF8.GetBytes("""
            {
              "__data_type": "UTD ",
              "Tag": { "type": "cexostring", "value": "sample" }
            }
            """)).Root;
    }
}
