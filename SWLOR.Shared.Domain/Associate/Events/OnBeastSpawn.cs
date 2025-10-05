using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnBeastSpawn : BaseEvent
    {
        public override string Script => ScriptName.OnBeastSpawn;
    }
}
