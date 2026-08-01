using Avalonia;
using Avalonia.Controls;

namespace SWLOR.Toolset.Editors.Behaviors
{
    /// <summary>
    /// Lays a field's label and its control side by side, or stacks them once the pane is too narrow
    /// to do both.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A two-column grid cannot give up its label column. Below a certain width the control's own
    /// minimum wins, the row asks for more than the pane has, and what a builder gets is not a
    /// smaller control but a clipped one - a search box reading "Search l", a spinner with its right
    /// half past the edge of the window. Widening the pane is the only fix a grid offers, and the
    /// pane is the thing that is not wide.
    /// </para>
    /// <para>
    /// So the row changes shape instead. Above the threshold it is the familiar label-and-control
    /// pair, aligned down the page with every other row. Below it the label moves above its control
    /// and the control gets the whole width - which is the same trade every settings page makes on a
    /// phone, and the only one that keeps the control usable rather than merely present.
    /// </para>
    /// </remarks>
    public sealed class LabeledFieldPanel : Panel
    {
        /// <summary>The label column when there is room for one.</summary>
        public static readonly StyledProperty<double> LabelWidthProperty =
            AvaloniaProperty.Register<LabeledFieldPanel, double>(nameof(LabelWidth), 150);

        /// <summary>Gap between label and control, across or down.</summary>
        public static readonly StyledProperty<double> SpacingProperty =
            AvaloniaProperty.Register<LabeledFieldPanel, double>(nameof(Spacing), 12);

        /// <summary>
        /// Width at which the row stops trying to fit both across. Set at the point where a label
        /// column plus a usable control stop coexisting: 150 for the label, and enough left over for
        /// a search box, a spinner and its buttons, or a row of picture tiles.
        /// </summary>
        public static readonly StyledProperty<double> StackBelowProperty =
            AvaloniaProperty.Register<LabeledFieldPanel, double>(nameof(StackBelow), 420);

        /// <summary>
        /// Whether the field reserves space for its label. Dedicated picker workspaces can turn
        /// this off when their surrounding card already identifies the value being edited.
        /// </summary>
        public static readonly StyledProperty<bool> ShowLabelProperty =
            AvaloniaProperty.Register<LabeledFieldPanel, bool>(nameof(ShowLabel), true);

        private bool _stacked;

        public double LabelWidth
        {
            get => GetValue(LabelWidthProperty);
            set => SetValue(LabelWidthProperty, value);
        }

        public double Spacing
        {
            get => GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        public double StackBelow
        {
            get => GetValue(StackBelowProperty);
            set => SetValue(StackBelowProperty, value);
        }

        public bool ShowLabel
        {
            get => GetValue(ShowLabelProperty);
            set => SetValue(ShowLabelProperty, value);
        }

        /// <summary>Whether this row is currently stacked, for tests and for callers that align to it.</summary>
        public bool IsStacked => _stacked;

        static LabeledFieldPanel()
        {
            AffectsMeasure<LabeledFieldPanel>(
                LabelWidthProperty,
                SpacingProperty,
                StackBelowProperty,
                ShowLabelProperty);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Children.Count < 2)
                return base.MeasureOverride(availableSize);

            var label = Children[0];
            var value = Children[1];

            if (!ShowLabel)
            {
                _stacked = false;
                label.Measure(new Size(0, 0));
                value.Measure(availableSize);
                return new Size(
                    double.IsInfinity(availableSize.Width)
                        ? value.DesiredSize.Width
                        : availableSize.Width,
                    value.DesiredSize.Height);
            }

            // An unconstrained width is a measure pass inside a scroll viewer or an auto-sized
            // parent, not a narrow pane: laying out stacked there would make every row stack.
            _stacked = !double.IsInfinity(availableSize.Width) && availableSize.Width < StackBelow;

            if (_stacked)
            {
                label.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                value.Measure(new Size(availableSize.Width, double.PositiveInfinity));

                return new Size(
                    Math.Max(label.DesiredSize.Width, value.DesiredSize.Width),
                    label.DesiredSize.Height + Spacing / 2 + value.DesiredSize.Height);
            }

            var valueWidth = Math.Max(0, availableSize.Width - LabelWidth - Spacing);
            label.Measure(new Size(LabelWidth, double.PositiveInfinity));
            value.Measure(new Size(valueWidth, double.PositiveInfinity));

            return new Size(
                double.IsInfinity(availableSize.Width)
                    ? LabelWidth + Spacing + value.DesiredSize.Width
                    : availableSize.Width,
                Math.Max(label.DesiredSize.Height, value.DesiredSize.Height));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children.Count < 2)
                return base.ArrangeOverride(finalSize);

            var label = Children[0];
            var value = Children[1];

            if (!ShowLabel)
            {
                label.Arrange(new Rect(0, 0, 0, 0));
                value.Arrange(new Rect(finalSize));
                return finalSize;
            }

            if (_stacked)
            {
                var labelHeight = label.DesiredSize.Height;
                label.Arrange(new Rect(0, 0, finalSize.Width, labelHeight));
                value.Arrange(new Rect(
                    0,
                    labelHeight + Spacing / 2,
                    finalSize.Width,
                    Math.Max(0, finalSize.Height - labelHeight - Spacing / 2)));

                return finalSize;
            }

            var valueWidth = Math.Max(0, finalSize.Width - LabelWidth - Spacing);
            label.Arrange(new Rect(0, 0, LabelWidth, finalSize.Height));
            value.Arrange(new Rect(LabelWidth + Spacing, 0, valueWidth, finalSize.Height));

            return finalSize;
        }
    }
}
