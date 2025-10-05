using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Dialog.Events
{
    public class OnDialogAppears : BaseEvent
    {
        public override string Script => DialogScriptName.OnDialogAppears;
    }
}
