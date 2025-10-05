using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Dialog.Events
{
    public class OnDialogAction : BaseEvent
    {
        public override string Script => ScriptName.OnDialogAction;
    }
}
