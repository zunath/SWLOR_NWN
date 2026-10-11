using Nwn.Authoring.Documents.NimGff;

namespace SWLOR.Toolset.Domain.Editors.Triggers
{
    /// <summary>SWLOR's trigger behaviors and Basic rows, as the shared trigger editor reads them.</summary>
    public sealed class SwlorTriggerBehaviorCatalog : ITriggerBehaviorCatalog
    {
        public static SwlorTriggerBehaviorCatalog Instance { get; } = new();

        private SwlorTriggerBehaviorCatalog()
        {
        }

        public IReadOnlyList<TriggerBehavior> All => TriggerBehaviorCatalog.All;

        public TriggerBehavior Custom => TriggerBehaviorCatalog.Custom;

        public IReadOnlyList<BehaviorFieldDefinition> BasicFields => TriggerEditorLayout.Basic;

        public TriggerBehavior Classify(JsonGffStruct trigger) => TriggerBehaviorCatalog.Classify(trigger);
    }
}
