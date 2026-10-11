using Nwn.Authoring.Documents.NimGff;

namespace SWLOR.Toolset.Domain.Editors.Sounds
{
    /// <summary>Ambient-sound accessors over the shared sound value store.</summary>
    public sealed class SoundValueStore : SoundBehaviorValueStore
    {
        public SoundValueStore(JsonGffStruct sound) : base(sound)
        {
        }
    }
}
