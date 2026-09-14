using Avalonia.Controls;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// The item editor's view. Its only code-behind duty is the responsive layout: the pane's own
    /// width decides how much room the fields still have to share with their fixed-width companions
    /// (the preview rail, the Flags card, the behavior rail), and on a small window those give their
    /// space back rather than squeezing every field into ellipses.
    /// </summary>
    /// <remarks>
    /// Applied imperatively rather than through width-triggered style classes on the root. Avalonia
    /// has no width query to begin with, and a descendant selector (<c>UserControl.narrow
    /// Border#FlagsCard</c>) does not re-evaluate when the ANCESTOR's classes change - the class
    /// lands on the root and nothing below it moves. Setting the affected properties here is what
    /// actually reflows, and it keeps the whole rule in one readable place.
    /// </remarks>
    public partial class ItemEditorView : UserControl
    {
        /// <summary>
        /// Below this the Flags card stacks under the fields and the rails shrink. Chosen from what
        /// the pane actually needs: the fixed companions take ~440px, and a field row wants roughly
        /// 420 beside its label before its choice lists start rendering as ellipses.
        /// </summary>
        private const double NarrowWidth = 900;

        /// <summary>Below this the preview rail goes entirely - by then it is the only thing left to give.</summary>
        private const double TinyWidth = 700;

        private const double RailWidth = 190;
        private const double NarrowRailWidth = 132;
        private const double BehaviorRailWidth = 210;
        private const double NarrowBehaviorRailWidth = 150;
        private const double FlagsCardWidth = 250;

        public ItemEditorView()
        {
            InitializeComponent();
            ApplyResponsiveLayout(Bounds.Width);
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            ApplyResponsiveLayout(e.NewSize.Width);
        }

        private void ApplyResponsiveLayout(double width)
        {
            // Zero width is the pre-layout measure pass, not a genuinely tiny pane: reacting to it
            // would flash the narrow layout open on every document.
            if (width <= 0)
                return;

            var narrow = width < NarrowWidth;
            var tiny = width < TinyWidth;

            if (this.FindControl<Border>("PreviewRail") is { } rail)
            {
                rail.Width = narrow ? NarrowRailWidth : RailWidth;
                rail.IsVisible = !tiny;
            }

            if (this.FindControl<Border>("FlagsCard") is { } flags)
            {
                // Out of the second column and onto its own row: the Auto column then measures to
                // zero, so the fields get the full width instead of sharing it with a 250px card.
                Grid.SetColumn(flags, narrow ? 0 : 1);
                Grid.SetRow(flags, narrow ? 1 : 0);
                flags.Width = narrow ? double.NaN : FlagsCardWidth;
                flags.Margin = narrow ? new Avalonia.Thickness(0, 10, 0, 0) : default;
            }

            if (this.FindControl<Behaviors.BehaviorRailView>("BehaviorRail") is { } behaviorRail)
                behaviorRail.Width = narrow ? NarrowBehaviorRailWidth : BehaviorRailWidth;
        }
    }
}
