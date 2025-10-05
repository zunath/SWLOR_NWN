using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Creature
{
    public class OnCreatureRoundEndAfter : BaseEvent
    {
        public override string Script => ScriptName.OnCreatureRoundEndAfter;
    }
}
