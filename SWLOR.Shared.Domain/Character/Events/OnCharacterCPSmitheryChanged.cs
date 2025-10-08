using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnCharacterCPSmitheryChanged : BaseEvent
    {
        public override string Script => CharacterScriptName.OnCharacterCPSmitheryChanged;
    }
}
