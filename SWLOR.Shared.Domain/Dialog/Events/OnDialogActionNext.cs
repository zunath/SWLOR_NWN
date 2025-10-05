using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Dialog.Events
{
    public class OnDialogActionNext : BaseEvent
    {
        public override string Script => ScriptName.OnDialogActionNext;
    }
}
