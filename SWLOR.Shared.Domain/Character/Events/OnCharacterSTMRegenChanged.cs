using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnCharacterSTMRegenChanged : BaseEvent
    {
        public override string Script => CharacterScriptName.OnCharacterSTMRegenChanged;
    }
}
