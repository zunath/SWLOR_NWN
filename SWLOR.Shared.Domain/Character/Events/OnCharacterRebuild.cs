using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnCharacterRebuild : BaseEvent
    {
        public override string Script => ScriptName.OnCharacterRebuild;
    }
}
