using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Dialog.Events
{
    public class OnDialogActionBack : BaseEvent
    {
        public override string Script => DialogScriptName.OnDialogActionBack;
    }
}
