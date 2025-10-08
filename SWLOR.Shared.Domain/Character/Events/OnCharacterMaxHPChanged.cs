using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnCharacterMaxHPChanged : BaseEvent
    {
        public override string Script => CharacterScriptName.OnCharacterMaxHPChanged;
    }
}
