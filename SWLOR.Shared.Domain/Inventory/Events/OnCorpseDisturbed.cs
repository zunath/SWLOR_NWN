using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Inventory.Events
{
    public class OnCorpseDisturbed : BaseEvent
    {
        public override string Script => ScriptName.OnCorpseDisturbed;
    }
}
