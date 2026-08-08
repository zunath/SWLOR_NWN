using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace SWLOR.Toolset.Shell.Controls
{
    /// <summary>
    /// Sizes every ComboBox popup for the option labels instead of forcing it to stay as narrow as
    /// the closed field. The Fluent template binds the popup's minimum width to the field width,
    /// which is appropriate for ordinary forms but clips the toolset's sentence-like choices.
    /// </summary>
    public static class ReadableComboBoxDropDowns
    {
        private const double MinimumReadableWidth = 280;
        private const double MaximumReadableWidth = 640;
        private const double PopupChromeWidth = 56;
        private const double TopLevelMargin = 32;
        private static bool _registered;

        /// <summary>Starts sizing ComboBox popups. Safe to call more than once.</summary>
        public static void Register()
        {
            if (_registered)
                return;

            _registered = true;
            ComboBox.IsDropDownOpenProperty.Changed.AddClassHandler<ComboBox>(OnDropDownOpenChanged);
        }

        private static void OnDropDownOpenChanged(ComboBox comboBox, AvaloniaPropertyChangedEventArgs e)
        {
            if (!comboBox.IsDropDownOpen)
                return;

            // The popup is supplied by the control template and is not guaranteed to have been
            // realized during the property notification. Size it after the open pass completes.
            Dispatcher.UIThread.Post(
                () => SizePopup(comboBox),
                DispatcherPriority.Background);
        }

        private static void SizePopup(ComboBox comboBox)
        {
            if (!comboBox.IsDropDownOpen)
                return;

            var popup = comboBox
                .GetVisualDescendants()
                .OfType<Popup>()
                .FirstOrDefault(candidate => candidate.Name == "PART_Popup");
            if (popup == null)
                return;

            var fieldWidth = Math.Max(0, comboBox.Bounds.Width);
            var availableWidth = TopLevel.GetTopLevel(comboBox)?.ClientSize.Width ?? MaximumReadableWidth;
            var maximumWidth = availableWidth > TopLevelMargin
                ? Math.Min(MaximumReadableWidth, availableWidth - TopLevelMargin)
                : MaximumReadableWidth;
            maximumWidth = Math.Max(fieldWidth, maximumWidth);

            var preferredWidth = Math.Max(fieldWidth, MinimumReadableWidth);
            var measurement = new TextBlock
            {
                FontFamily = comboBox.FontFamily,
                FontSize = comboBox.FontSize,
                FontStyle = comboBox.FontStyle,
                FontWeight = comboBox.FontWeight,
                TextWrapping = Avalonia.Media.TextWrapping.NoWrap
            };
            foreach (var item in comboBox.ItemsView)
            {
                var label = DisplayLabel(item);
                if (string.IsNullOrWhiteSpace(label))
                    continue;

                // Reuse one measuring control for every label. Character count is not a safe proxy
                // in a proportional font ("WWW" can be wider than a longer run of "i"), while one
                // TextBlock avoids allocating thousands of controls for a large 2DA choice list.
                measurement.Text = label;
                measurement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                preferredWidth = Math.Max(
                    preferredWidth,
                    measurement.DesiredSize.Width + PopupChromeWidth);
            }

            // A local value deliberately replaces the Fluent template's binding to the closed
            // field width. Very long labels stop growing at a comfortable maximum and wrap via the
            // global ComboBoxItem style instead of disappearing behind the popup edge.
            popup.MinWidth = Math.Min(preferredWidth, maximumWidth);
            popup.MaxWidth = maximumWidth;
        }

        private static string? DisplayLabel(object? item) => item switch
        {
            null => null,
            ComboBoxItem comboBoxItem => comboBoxItem.Content?.ToString(),
            _ => item.ToString()
        };
    }
}
