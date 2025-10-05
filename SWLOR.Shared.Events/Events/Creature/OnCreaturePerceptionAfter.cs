using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Creature
{
    public class OnCreaturePerceptionAfter : BaseEvent
    {
        public override string Script => ScriptName.OnCreaturePerceptionAfter;
    }
}
