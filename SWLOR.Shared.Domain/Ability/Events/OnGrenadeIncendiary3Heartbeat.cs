using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Ability.Events
{
    public class OnGrenadeIncendiary3Heartbeat : BaseEvent
    {
        public override string Script => AbilityScriptName.OnGrenadeIncendiary3Heartbeat;
    }
}
