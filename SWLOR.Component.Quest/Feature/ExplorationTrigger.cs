using SWLOR.Shared.Domain.Quest.Events;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Quest.Feature
{
    public class ExplorationTrigger
    {
        [ScriptHandler<OnExploreTrigger>]
        public void EnterExplorationTrigger()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            const string VariableName = "TRIGGER_ID";
            var trigger = OBJECT_SELF;
            var triggerId = GetLocalString(trigger, VariableName);
            if (string.IsNullOrWhiteSpace(triggerId))
            {
                triggerId = Guid.NewGuid().ToString();
                SetLocalString(trigger, VariableName, triggerId);
            }

            // Player has already seen this exploration trigger this server reboot.
            if (GetLocalBool(player, triggerId)) return;

            var message = GetLocalString(trigger, "DISPLAY_TEXT");
            SendMessageToPC(player, ColorToken.Cyan(message));
            SetLocalBool(player, triggerId, true);
            
            AssignCommand(player, () => PlaySound("gui_prompt"));
        }
    }
}
