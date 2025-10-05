using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Inventory.Events
{
    public class OnOpenTrainingStore : BaseEvent
    {
        public override string Script => ScriptName.OnOpenTrainingStore;
    }
}
