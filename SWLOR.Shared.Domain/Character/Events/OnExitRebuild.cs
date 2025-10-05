using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnExitRebuild : BaseEvent
    {
        public override string Script => CharacterScriptName.OnExitRebuild;
    }
}
