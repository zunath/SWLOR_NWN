using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnCharacterDefenseChanged : BaseEvent
    {
        public override string Script => CharacterScriptName.OnCharacterDefenseChanged;
    }
}
