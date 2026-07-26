using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Doors;

namespace SWLOR.Toolset.Editors.Doors
{
    /// <summary>One group heading, divider, or selectable door behavior in the left rail.</summary>
    public sealed partial class DoorBehaviorListItemViewModel : ObservableObject
    {
        public DoorBehavior? Behavior { get; }

        public string Text { get; }

        public bool IsHeader => Behavior == null && !IsRule;

        public bool IsRule { get; private init; }

        public bool IsSelectable => Behavior != null;

        [ObservableProperty]
        private bool _isSelected;

        private DoorBehaviorListItemViewModel(DoorBehavior? behavior, string text)
        {
            Behavior = behavior;
            Text = text;
        }

        public static DoorBehaviorListItemViewModel Header(string title) => new(null, title);

        public static DoorBehaviorListItemViewModel Rule() => new(null, string.Empty) { IsRule = true };

        public static DoorBehaviorListItemViewModel For(DoorBehavior behavior) =>
            new(behavior, behavior.DisplayName);
    }
}
