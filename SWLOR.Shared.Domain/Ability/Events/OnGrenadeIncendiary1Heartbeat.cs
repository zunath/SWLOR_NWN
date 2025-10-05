using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Ability.Events
{
    public class OnGrenadeIncendiary1Heartbeat : BaseEvent
    {
        public override string Script => AbilityScriptName.OnGrenadeIncendiary1Heartbeat;
    }
}
