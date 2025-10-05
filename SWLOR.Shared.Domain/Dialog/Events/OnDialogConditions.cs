using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Dialog.Events
{
    public class OnDialogConditions : BaseEvent
    {
        public override string Script => ScriptName.OnDialogConditions;
    }
}
