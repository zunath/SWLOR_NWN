using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Input;

namespace SWLOR.ContentBuilder.Windows
{
    /// <summary>
    /// Wires a Slider and a small numeric TextBox together so either one can drive the value:
    /// moving the slider live-updates the box text, and typing a number into the box (committed on
    /// Enter or LostFocus) moves the slider. This is the single reusable mechanism behind every
    /// slider's value readout in MainWindow — callers should not hand-roll their own TextChanged /
    /// ValueChanged pair for a slider+box.
    ///
    /// Values are clamped to the slider's Minimum/Maximum on commit; unparsable input reverts the
    /// box back to the slider's current value instead of being accepted. The box only ever shows a
    /// plain integer (no "%" or other suffix) — percent-style sliders (Loop Factor, Organic Fill,
    /// Accent Density) are meant to be typed as a plain number such as "35" for 35%; any suffix is
    /// rendered by a separate adjacent label, not inside the editable text.
    /// </summary>
    internal static class SliderTextBoxSync
    {
        public static void Attach(Slider slider, TextBox box)
        {
            box.Text = FormatValue(slider);

            // Keep the box in sync with the slider regardless of what moved it (drag, keyboard,
            // programmatic Value assignment from theme/profile defaults, or a committed box edit).
            slider.ValueChanged += (_, _) => box.Text = FormatValue(slider);

            box.PreviewKeyDown += (_, e) =>
            {
                if (e.Key != Key.Enter) return;
                Commit(slider, box);
                e.Handled = true;
            };

            box.LostFocus += (_, _) => Commit(slider, box);
        }

        private static void Commit(Slider slider, TextBox box)
        {
            if (double.TryParse(box.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                var clamped = Math.Round(Math.Max(slider.Minimum, Math.Min(slider.Maximum, parsed)));
                slider.Value = clamped;
            }

            // Whether parsing succeeded or not, snap the box back to the (possibly unchanged)
            // slider value so invalid input never lingers in the box.
            box.Text = FormatValue(slider);
        }

        private static string FormatValue(Slider slider) => ((int)slider.Value).ToString(CultureInfo.InvariantCulture);
    }
}
