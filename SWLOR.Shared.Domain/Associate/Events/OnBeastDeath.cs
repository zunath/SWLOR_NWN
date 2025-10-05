using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnBeastDeath : BaseEvent
    {
        public override string Script => ScriptName.OnBeastDeath;
    }
}
