using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Sounds;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Sounds
{
    /// <summary>
    /// One row of the ambient-sound editor: the shared behavior row plus the ordered Sounds list,
    /// which is the only control no other editor has.
    /// </summary>
    public sealed class SoundRowViewModel : BehaviorRowViewModel
    {
        public bool IsSoundList => Definition.Kind == BehaviorFieldKind.SoundList;

        public SoundListEditorViewModel? SoundList { get; }

        /// <summary>
        /// A sound's palette category is stored, not defaulted: an absent field means the blueprint
        /// has never been filed, and showing the first category would claim otherwise.
        /// </summary>
        protected override bool SelectsFirstChoiceWhenUnset => false;

        public override bool HasValue => IsSoundList
            ? SoundList is { HasValidCount: true }
            : base.HasValue;

        public SoundRowViewModel(
            BehaviorFieldDefinition definition,
            SoundValueStore store,
            Func<string, Action, bool> runEdit,
            IReadOnlyList<BehaviorChoice> choices,
            IReadOnlyList<string> audioResources,
            Action changed,
            Services.SoundPreviewService? preview = null)
            : base(definition, store, runEdit, choices, changed)
        {
            if (IsSoundList)
            {
                SoundList = new SoundListEditorViewModel(
                    store, runEdit, audioResources, definition.MaxItems, OnListChanged, preview);
            }

            Reload();
        }

        protected override void ReadValue()
        {
            if (IsSoundList)
            {
                SoundList?.Reload();
                return;
            }

            base.ReadValue();
        }

        private void OnListChanged()
        {
            NotifyValueShapeChanged();
            OnApplied();
        }
    }
}
