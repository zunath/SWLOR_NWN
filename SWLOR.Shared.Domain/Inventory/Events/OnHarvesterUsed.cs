using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Inventory.Events
{
    public class OnHarvesterUsed : BaseEvent
    {
        public override string Script => ScriptName.OnHarvesterUsed;
    }
}
