using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Triggers;

namespace SWLOR.Toolset.Editors.Triggers
{
    /// <summary>
    /// One line of the behavior list: either a group heading ("WORLD") or a selectable behavior.
    /// Headings share the list so the rail is a single flat ItemsControl rather than a nested tree
    /// that would have to be kept in selection sync.
    /// </summary>
    public sealed partial class TriggerBehaviorListItemViewModel : ObservableObject
    {
        public TriggerBehavior? Behavior { get; }

        public string Text { get; }

        public bool IsHeader => Behavior == null;

        public bool IsSelectable => Behavior != null;

        /// <summary>Trailing clause on the row, as in "None — plain trigger".</summary>
        public string? Tagline => Behavior?.Tagline;

        public bool HasTagline => !string.IsNullOrEmpty(Behavior?.Tagline);

        [ObservableProperty]
        private bool _isSelected;

        private TriggerBehaviorListItemViewModel(TriggerBehavior? behavior, string text)
        {
            Behavior = behavior;
            Text = text;
        }

        public static TriggerBehaviorListItemViewModel Header(string title) => new(null, title);

        public static TriggerBehaviorListItemViewModel For(TriggerBehavior behavior) =>
            new(behavior, behavior.DisplayName);
    }
}
