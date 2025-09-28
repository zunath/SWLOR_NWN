using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Dialog
{
    public class OnDialogActions : BaseEvent
    {
        public override string Script => ScriptName.OnDialogActions;
    }
}
