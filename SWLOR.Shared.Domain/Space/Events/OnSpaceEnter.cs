using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Space.Events
{
    public class OnSpaceEnter : BaseEvent
    {
        public override string Script => SpaceScriptName.OnSpaceEnter;
    }
}
