using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Dialog.Events
{
    public class OnDialogEnd : BaseEvent
    {
        public override string Script => DialogScriptName.OnDialogEnd;
    }
}
