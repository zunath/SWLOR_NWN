using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Behaviors
{
    /// <summary>
    /// One line of a behavior editor's left rail: a group heading ("WORLD"), a divider, or a
    /// selectable behavior.
    /// </summary>
    /// <remarks>
    /// Headings and dividers ride in the same flat list as the behaviors, so the rail is a single
    /// ItemsControl rather than a nested tree that would have to be kept in selection sync. One class
    /// serves the trigger, waypoint, door, and sound editors: they all draw the same row from the
    /// same six facts, which <see cref="IBehaviorDescriptor"/> names.
    /// </remarks>
    public sealed partial class BehaviorListItemViewModel : ObservableObject
    {
        /// <summary>Null on a heading or divider row.</summary>
        public IBehaviorDescriptor? Behavior { get; }

        public string Text { get; }

        public bool IsHeader => Behavior == null && !IsRule;

        /// <summary>A plain divider, separating the ungrouped entries from the grouped ones.</summary>
        public bool IsRule { get; private init; }

        public bool IsSelectable => Behavior != null;

        /// <summary>Trailing clause on the row, as in "None — plain trigger".</summary>
        public string? Tagline => Behavior?.Tagline;

        public bool HasTagline => !string.IsNullOrEmpty(Behavior?.Tagline);

        [ObservableProperty]
        private bool _isSelected;

        private BehaviorListItemViewModel(IBehaviorDescriptor? behavior, string text)
        {
            Behavior = behavior;
            Text = text;
        }

        public static BehaviorListItemViewModel Header(string title) => new(null, title);

        public static BehaviorListItemViewModel Rule() => new(null, string.Empty) { IsRule = true };

        public static BehaviorListItemViewModel For(IBehaviorDescriptor behavior) =>
            new(behavior, behavior.DisplayName);

        /// <summary>
        /// Fills <paramref name="target"/> with the rail for a catalog, inserting a heading when a
        /// group starts and a divider when a run of grouped behaviors ends.
        /// </summary>
        /// <remarks>
        /// An ungrouped behavior ends the run it follows rather than joining it. Custom has no group,
        /// and without the divider it rendered under whichever heading happened to come last — which
        /// is how it once ended up filed as a hazard.
        /// </remarks>
        public static void Build(
            ObservableCollection<BehaviorListItemViewModel> target,
            IEnumerable<IBehaviorDescriptor> behaviors)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(behaviors);

            target.Clear();

            string? group = null;
            foreach (var behavior in behaviors)
            {
                if (behavior.Group == null && group != null)
                {
                    target.Add(Rule());
                    group = null;
                }
                else if (behavior.Group != null &&
                         !string.Equals(behavior.Group, group, StringComparison.Ordinal))
                {
                    target.Add(Header(behavior.Group));
                    group = behavior.Group;
                }

                target.Add(For(behavior));
            }
        }

        /// <summary>Marks whichever row names <paramref name="behaviorId"/> as the selected one.</summary>
        public static void Select(
            IEnumerable<BehaviorListItemViewModel> items,
            string behaviorId)
        {
            ArgumentNullException.ThrowIfNull(items);

            foreach (var item in items)
                item.IsSelected = item.Behavior?.Id == behaviorId;
        }
    }
}
