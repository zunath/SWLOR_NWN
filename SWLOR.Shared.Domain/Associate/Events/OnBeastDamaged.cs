using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnBeastDamaged : BaseEvent
    {
        public override string Script => ScriptName.OnBeastDamaged;
    }
}
