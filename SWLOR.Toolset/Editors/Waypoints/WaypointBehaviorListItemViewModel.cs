using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Waypoints;

namespace SWLOR.Toolset.Editors.Waypoints
{
    public sealed partial class WaypointBehaviorListItemViewModel : ObservableObject
    {
        public WaypointBehavior? Behavior { get; }

        public string Text { get; }

        public bool IsHeader => Behavior == null && !IsRule;

        public bool IsRule { get; private init; }

        public bool IsSelectable => Behavior != null;

        [ObservableProperty]
        private bool _isSelected;

        private WaypointBehaviorListItemViewModel(WaypointBehavior? behavior, string text)
        {
            Behavior = behavior;
            Text = text;
        }

        public static WaypointBehaviorListItemViewModel Header(string title) => new(null, title);

        public static WaypointBehaviorListItemViewModel Rule() =>
            new(null, string.Empty) { IsRule = true };

        public static WaypointBehaviorListItemViewModel For(WaypointBehavior behavior) =>
            new(behavior, behavior.DisplayName);
    }
}
