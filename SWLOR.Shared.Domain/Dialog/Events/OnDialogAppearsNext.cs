using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Dialog.Events
{
    public class OnDialogAppearsNext : BaseEvent
    {
        public override string Script => ScriptName.OnDialogAppearsNext;
    }
}
