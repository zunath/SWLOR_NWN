using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Ability.Events
{
    public class OnBurstOfSpeedApply : BaseEvent
    {
        public override string Script => AbilityScriptName.OnBurstOfSpeedApply;
    }
}
