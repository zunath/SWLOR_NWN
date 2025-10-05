using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnBuyStatRebuild : BaseEvent
    {
        public override string Script => ScriptName.OnBuyStatRebuild;
    }
}
