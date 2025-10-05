using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnBeastBlocked : BaseEvent
    {
        public override string Script => ScriptName.OnBeastBlocked;
    }
}
