using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnAssociateStateEffect : BaseEvent
    {
        public override string Script => ScriptName.OnAssociateStateEffect;
    }
}
