using Nwn.Authoring.Documents.NimGff;

namespace SWLOR.Toolset.Domain.Editors.Doors
{
    /// <summary>SWLOR's door behaviors, Basic rows and conventions, as the shared door editor reads them.</summary>
    public sealed class SwlorDoorBehaviorCatalog : IDoorBehaviorCatalog
    {
        public static SwlorDoorBehaviorCatalog Instance { get; } = new();

        private SwlorDoorBehaviorCatalog()
        {
        }

        public IReadOnlyList<DoorBehavior> All => DoorBehaviorCatalog.All;

        public DoorBehavior Custom => DoorBehaviorCatalog.Custom;

        public IReadOnlyList<DoorFieldDefinition> BasicFields => DoorEditorLayout.Basic;

        public DoorScriptConventions Conventions => DoorValueStore.SwlorConventions;

        public DoorBehavior Classify(JsonGffStruct door) => DoorBehaviorCatalog.Classify(door);
    }
}
