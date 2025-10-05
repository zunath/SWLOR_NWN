using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Creature
{
    public class OnCreatureDamagedBefore : BaseEvent
    {
        public override string Script => ScriptName.OnCreatureDamagedBefore;
    }
}
