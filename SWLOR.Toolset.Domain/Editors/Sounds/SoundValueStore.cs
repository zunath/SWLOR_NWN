using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Sounds
{
    /// <summary>Ambient-sound accessors over the shared behavior value store.</summary>
    public sealed class SoundValueStore : BehaviorValueStore
    {
        public const string SoundsField = "Sounds";
        public const string SoundEntryField = "Sound";

        public SoundValueStore(JsonGffStruct sound) : base(sound)
        {
        }

        public JsonGffStruct Sound => ValueStruct;

        public IReadOnlyList<string> GetSounds() => GetResRefList(SoundsField, SoundEntryField);

        public void AddSound(string resRef) =>
            AddResRefListEntry(SoundsField, SoundEntryField, resRef);

        public void RemoveSound(int index) => RemoveListEntry(SoundsField, index);

        public void MoveSound(int fromIndex, int toIndex) =>
            MoveListEntry(SoundsField, fromIndex, toIndex);

        public void ReplaceSounds(IEnumerable<string> resRefs) =>
            ReplaceResRefList(SoundsField, SoundEntryField, resRefs);

        public void Clear(SoundBehavior behavior)
        {
            ArgumentNullException.ThrowIfNull(behavior);
            var fields = behavior.Id == SoundBehaviorCatalog.CustomId
                ? behavior.Fields.Concat(SoundEditorLayout.Advanced.Where(field => field.CustomOnly))
                : behavior.Fields;
            Clear(behavior.Manages, fields);
        }
    }
}
