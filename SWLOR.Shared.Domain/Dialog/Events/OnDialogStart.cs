using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Dialog.Events
{
    public class OnDialogStart : BaseEvent
    {
        public override string Script => DialogScriptName.OnDialogStart;
    }
}
