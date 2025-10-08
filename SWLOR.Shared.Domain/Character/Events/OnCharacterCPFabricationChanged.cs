using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnCharacterCPFabricationChanged : BaseEvent
    {
        public override string Script => CharacterScriptName.OnCharacterCPFabricationChanged;
    }
}
