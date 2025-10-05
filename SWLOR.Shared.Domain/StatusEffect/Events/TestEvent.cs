using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.StatusEffect.Events
{
    public class TestEvent : BaseEvent
    {
        public override string Script => "test";
    }
}
