using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Placeables;

namespace SWLOR.Toolset.Editors.Placeables
{
    /// <summary>One row of the behavior list, or the heading above a run of them.</summary>
    /// <remarks>
    /// Headings ride in the same list rather than nesting the rows in groups, so the whole set stays
    /// on screen at once - which is the point of the tab. A heading is not selectable.
    /// </remarks>
    public partial class BehaviorListItemViewModel : ObservableObject
    {
        private BehaviorListItemViewModel(PlaceableBehavior? behavior, string text, bool isHeader)
        {
            Behavior = behavior;
            Text = text;
            IsHeader = isHeader;
        }

        public static BehaviorListItemViewModel ForBehavior(PlaceableBehavior behavior) =>
            new(behavior, behavior.Name, isHeader: false);

        public static BehaviorListItemViewModel ForHeader(string title) =>
            new(null, title, isHeader: true);

        /// <summary>Null on a heading row.</summary>
        public PlaceableBehavior? Behavior { get; }

        public string Text { get; }

        public bool IsHeader { get; }

        public bool IsSelectable => !IsHeader;
    }
}
