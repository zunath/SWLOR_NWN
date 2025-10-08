using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnCharacterFPRegenChanged : BaseEvent
    {
        public override string Script => CharacterScriptName.OnCharacterFPRegenChanged;
    }
}
