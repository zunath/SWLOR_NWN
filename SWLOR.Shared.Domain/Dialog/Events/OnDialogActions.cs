using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Dialog.Events
{
    public class OnDialogActions : BaseEvent
    {
        public override string Script => ScriptName.OnDialogActions;
    }
}
