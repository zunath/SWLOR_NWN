using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Ability.Events
{
    public class OnBurstOfSpeedRemoved : BaseEvent
    {
        public override string Script => AbilityScriptName.OnBurstOfSpeedRemoved;
    }
}
