using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnCharacterHPRegenChanged : BaseEvent
    {
        public override string Script => CharacterScriptName.OnCharacterHPRegenChanged;
    }
}
