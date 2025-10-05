using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Space.Events
{
    public class OnPlayerHullAdjusted : BaseEvent
    {
        public override string Script => SpaceScriptName.OnPlayerHullAdjusted;
    }
}
