using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnInputAttackObjectAfter : BaseEvent
    {
        public override string Script => ScriptName.OnInputAttackObjectAfter;
    }
}
