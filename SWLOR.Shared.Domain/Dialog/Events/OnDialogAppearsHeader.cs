using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Dialog.Events
{
    public class OnDialogAppearsHeader : BaseEvent
    {
        public override string Script => ScriptName.OnDialogAppearsHeader;
    }
}
