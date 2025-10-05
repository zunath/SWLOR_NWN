using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Ability.Events
{
    public class OnGrenadeIncendiary2Heartbeat : BaseEvent
    {
        public override string Script => AbilityScriptName.OnGrenadeIncendiary2Heartbeat;
    }
}
