using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.World.Events
{
    public class OnOpenHoloNet : BaseEvent
    {
        public override string Script => WorldScriptName.OnOpenHoloNet;
    }
}
