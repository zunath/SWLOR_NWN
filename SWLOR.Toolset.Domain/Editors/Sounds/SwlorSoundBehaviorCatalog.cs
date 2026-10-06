using Nwn.Authoring.Documents.NimGff;

namespace SWLOR.Toolset.Domain.Editors.Sounds
{
    /// <summary>SWLOR's ambient-sound behaviors and Basic rows, as the shared sound editor reads them.</summary>
    public sealed class SwlorSoundBehaviorCatalog : ISoundBehaviorCatalog
    {
        public static SwlorSoundBehaviorCatalog Instance { get; } = new();

        private SwlorSoundBehaviorCatalog()
        {
        }

        public IReadOnlyList<SoundBehavior> All => SoundBehaviorCatalog.All;

        public SoundBehavior Custom => SoundBehaviorCatalog.Custom;

        public IReadOnlyList<BehaviorFieldDefinition> BasicFields => SoundEditorLayout.Basic;

        public SoundBehavior Classify(JsonGffStruct sound) => SoundBehaviorCatalog.Classify(sound);
    }
}
