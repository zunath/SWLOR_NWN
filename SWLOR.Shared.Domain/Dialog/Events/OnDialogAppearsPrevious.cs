using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Dialog.Events
{
    public class OnDialogAppearsPrevious : BaseEvent
    {
        public override string Script => DialogScriptName.OnDialogAppearsPrevious;
    }
}
