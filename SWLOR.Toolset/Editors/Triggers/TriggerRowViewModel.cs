using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Triggers
{
    /// <summary>
    /// One row of the trigger editor: the shared behavior row plus the one thing only a trigger
    /// needs - a live answer for a destination tag.
    /// </summary>
    /// <remarks>
    /// The load-screen picker is not here any more. Doors need the same picture gallery for their
    /// appearances and portraits, so it belongs to the shared row rather than to this one.
    /// </remarks>
    public sealed class TriggerRowViewModel : BehaviorRowViewModel
    {
        private readonly Func<BehaviorTagScope, string, string?>? _resolveTag;

        protected override bool SelectsFirstChoiceWhenUnset =>
            Definition.Name != "LinkedToFlags";

        public TriggerRowViewModel(
            BehaviorFieldDefinition definition,
            BehaviorValueStore store,
            Func<string, Action, bool> runEdit,
            Func<BehaviorTagScope, string, string?>? resolveTag,
            IReadOnlyList<BehaviorChoice>? choices = null,
            ChoicePreviewService? previews = null,
            Action? valueChanged = null)
            : base(definition, store, runEdit, choices, valueChanged, previews)
        {
            _resolveTag = resolveTag;
            Reload();
        }

        protected override void OnApplied()
        {
            base.OnApplied();
            RefreshStatus();
        }

        /// <summary>
        /// A tag row says which area its target lives in - the check that catches a doorway pointing
        /// at a tag no area defines.
        /// </summary>
        public override void RefreshStatus()
        {
            if (Definition.Kind != BehaviorFieldKind.TagReference || Text.Length == 0 ||
                _resolveTag == null)
            {
                // No "required" here: the label already carries that badge, and having both put two
                // pieces of text in the same row from opposite ends, which is what collided.
                IsStatusGood = true;
                Status = null;
                return;
            }

            var scope = Definition.TagScope;
            if (Definition.Name == "LinkedTo")
            {
                scope = Store.GetInteger(BehaviorFieldStorage.Field, "LinkedToFlags") switch
                {
                    1 => BehaviorTagScope.Door,
                    2 => BehaviorTagScope.Waypoint,
                    _ => BehaviorTagScope.None
                };
            }

            if (scope == BehaviorTagScope.None)
            {
                IsStatusGood = false;
                Status = "✗ choose whether the destination is a waypoint or door";
                return;
            }

            var area = _resolveTag(scope, Text);
            IsStatusGood = area != null;
            Status = area != null
                ? $"✓ in {area}"
                : scope switch
                {
                    BehaviorTagScope.Waypoint => "✗ no waypoint defines this tag",
                    BehaviorTagScope.Door => "✗ no door defines this tag",
                    _ => "✗ no waypoint or door defines this tag"
                };
        }
    }
}
