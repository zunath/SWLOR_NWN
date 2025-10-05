using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Creature
{
    public class OnCreatureAggroEnter : BaseEvent
    {
        public override string Script => ScriptName.OnCreatureAggroEnter;
    }
}
