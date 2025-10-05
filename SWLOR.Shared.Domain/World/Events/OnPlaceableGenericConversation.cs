using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.World.Events
{
    public class OnPlaceableGenericConversation : BaseEvent
    {
        public override string Script => ScriptName.OnPlaceableGenericConversation;
    }
}