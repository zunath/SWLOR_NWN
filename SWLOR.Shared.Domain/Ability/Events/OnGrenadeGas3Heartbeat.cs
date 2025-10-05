using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Ability.Events
{
    public class OnGrenadeGas3Heartbeat : BaseEvent
    {
        public override string Script => AbilityScriptName.OnGrenadeGas3Heartbeat;
    }
}
