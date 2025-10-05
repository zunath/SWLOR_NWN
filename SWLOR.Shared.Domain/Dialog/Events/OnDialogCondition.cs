using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Dialog.Events
{
    public class OnDialogCondition : BaseEvent
    {
        public override string Script => DialogScriptName.OnDialogCondition;
    }
}
