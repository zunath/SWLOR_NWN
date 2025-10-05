using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnRestTriggerEnter : BaseEvent
    {
        public override string Script => CharacterScriptName.OnRestTriggerEnter;
    }
}
