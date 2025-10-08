using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnCharacterEvasionChanged : BaseEvent
    {
        public override string Script => CharacterScriptName.OnCharacterEvasionChanged;
    }
}
