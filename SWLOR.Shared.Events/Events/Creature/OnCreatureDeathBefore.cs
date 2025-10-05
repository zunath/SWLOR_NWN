using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Creature
{
    public class OnCreatureDeathBefore : BaseEvent
    {
        public override string Script => ScriptName.OnCreatureDeathBefore;
    }
}
