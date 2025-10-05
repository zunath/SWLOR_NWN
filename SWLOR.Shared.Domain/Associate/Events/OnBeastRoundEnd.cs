using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnBeastRoundEnd : BaseEvent
    {
        public override string Script => ScriptName.OnBeastRoundEnd;
    }
}
