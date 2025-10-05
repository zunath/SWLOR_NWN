using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Dialog.Events
{
    public class OnDialogAppear : BaseEvent
    {
        public override string Script => DialogScriptName.OnDialogAppear;
    }
}
