using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnBeastConversation : BaseEvent
    {
        public override string Script => AssociateScriptName.OnBeastConversation;
    }
}
