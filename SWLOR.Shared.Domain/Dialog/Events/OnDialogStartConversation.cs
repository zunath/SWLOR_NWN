using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Dialog.Events
{
    public class OnDialogStartConversation : BaseEvent
    {
        public override string Script => ScriptName.OnDialogStartConversation;
    }
}
