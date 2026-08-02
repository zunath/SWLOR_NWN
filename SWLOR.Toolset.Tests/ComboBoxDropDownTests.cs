using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;

namespace SWLOR.Toolset.Tests
{
    public sealed class ComboBoxDropDownTests
    {
        [AvaloniaTest]
        public void NarrowComboBoxesOpenAReadablePopupGlobally()
        {
            var first = new ComboBox
            {
                Width = 90,
                ItemsSource = new[] { "Short", "Speaker name used by this conversation" }
            };
            var second = new ComboBox
            {
                Width = 120,
                ItemsSource = new[] { "Normal", "A separate editor with another long choice" }
            };
            var window = new Window
            {
                Width = 900,
                Height = 500,
                Content = new StackPanel { Children = { first, second } }
            };

            try
            {
                window.Show();
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();

                AssertPopupIsReadable(first);
                first.IsDropDownOpen = false;
                Dispatcher.UIThread.RunJobs();
                AssertPopupIsReadable(second);
            }
            finally
            {
                window.Close();
            }
        }

        private static void AssertPopupIsReadable(ComboBox comboBox)
        {
            comboBox.IsDropDownOpen = true;
            Dispatcher.UIThread.RunJobs();

            var popup = comboBox
                .GetVisualDescendants()
                .OfType<Popup>()
                .Single(candidate => candidate.Name == "PART_Popup");

            popup.MinWidth.Should().BeGreaterThan(
                comboBox.Bounds.Width,
                "the popup must size to its labels rather than inherit a narrow field width");
            popup.MinWidth.Should().BeGreaterThanOrEqualTo(280);
            popup.MaxWidth.Should().BeLessThanOrEqualTo(640);

            var itemPresenters = popup.Child!
                .GetVisualDescendants()
                .OfType<ContentPresenter>()
                .Where(presenter => presenter.GetVisualAncestors().OfType<ComboBoxItem>().Any())
                .ToArray();
            itemPresenters.Should().NotBeEmpty();
            itemPresenters.Should().OnlyContain(
                    presenter => presenter.TextWrapping == TextWrapping.Wrap &&
                                 presenter.TextTrimming == TextTrimming.None,
                    "labels beyond the safe popup width must wrap rather than be clipped");
        }
    }
}
