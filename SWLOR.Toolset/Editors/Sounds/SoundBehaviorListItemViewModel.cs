using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Sounds;

namespace SWLOR.Toolset.Editors.Sounds
{
    /// <summary>A group heading, divider, or selectable behavior in the sound editor rail.</summary>
    public sealed partial class SoundBehaviorListItemViewModel : ObservableObject
    {
        public SoundBehavior? Behavior { get; }

        public string Text { get; }

        public bool IsHeader => Behavior == null && !IsRule;

        public bool IsRule { get; private init; }

        public bool IsSelectable => Behavior != null;

        [ObservableProperty]
        private bool _isSelected;

        private SoundBehaviorListItemViewModel(SoundBehavior? behavior, string text)
        {
            Behavior = behavior;
            Text = text;
        }

        public static SoundBehaviorListItemViewModel Header(string title) => new(null, title);

        public static SoundBehaviorListItemViewModel Rule() =>
            new(null, string.Empty) { IsRule = true };

        public static SoundBehaviorListItemViewModel For(SoundBehavior behavior) =>
            new(behavior, behavior.DisplayName);
    }
}
