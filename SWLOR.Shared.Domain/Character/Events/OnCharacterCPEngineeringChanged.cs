using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnCharacterCPEngineeringChanged : BaseEvent
    {
        public override string Script => CharacterScriptName.OnCharacterCPEngineeringChanged;
    }
}
