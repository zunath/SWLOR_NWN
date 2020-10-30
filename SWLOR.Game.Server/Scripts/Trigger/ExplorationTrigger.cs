using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Trigger
{
    public class ExplorationTrigger: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWCreature oPC = (NWScript.GetEnteringObject());
            if (!oPC.IsPlayer) return;

            var triggerID = NWScript.GetLocalString(NWScript.OBJECT_SELF, "TRIGGER_ID");
            if (string.IsNullOrWhiteSpace(triggerID))
            {
                triggerID = Guid.NewGuid().ToString();
                NWScript.SetLocalString(NWScript.OBJECT_SELF, "TRIGGER_ID", triggerID);
            }

            if (NWScript.GetLocalInt(oPC.Object, triggerID) == 1) return;

            var message = NWScript.GetLocalString(NWScript.OBJECT_SELF, "DISPLAY_TEXT");
            NWScript.SendMessageToPC(oPC.Object, ColorTokenService.Cyan(message));
            NWScript.SetLocalInt(oPC.Object, triggerID, 1);

            NWScript.AssignCommand(oPC.Object, () => NWScript.PlaySound("gui_prompt"));

        }
    }
}
