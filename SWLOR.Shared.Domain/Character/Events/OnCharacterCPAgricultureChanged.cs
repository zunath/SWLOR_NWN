using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnCharacterCPAgricultureChanged : BaseEvent
    {
        public override string Script => CharacterScriptName.OnCharacterCPAgricultureChanged;
    }
}
