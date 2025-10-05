using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Creature
{
    public class OnCreatureRestedAfter : BaseEvent
    {
        public override string Script => ScriptName.OnCreatureRestedAfter;
    }
}
