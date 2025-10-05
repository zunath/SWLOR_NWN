using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Combat.Events
{
    public class OnPlaceableDeath : BaseEvent
    {
        public override string Script => ScriptName.OnPlaceableDeath;
    }
}
