using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnCharacterMaxFPChanged : BaseEvent
    {
        public override string Script => CharacterScriptName.OnCharacterMaxFPChanged;
    }
}
