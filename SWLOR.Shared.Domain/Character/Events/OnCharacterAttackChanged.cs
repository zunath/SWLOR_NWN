using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnCharacterAttackChanged : BaseEvent
    {
        public override string Script => CharacterScriptName.OnCharacterAttackChanged;
    }
}
