using System;
using NWN;
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
            NWCreature oPC = (_.GetEnteringObject());
            if (!oPC.IsPlayer) return;

            string triggerID = _.GetLocalString(_.OBJECT_SELF, "TRIGGER_ID");
            if (string.IsNullOrWhiteSpace(triggerID))
            {
                triggerID = Guid.NewGuid().ToString();
                _.SetLocalString(_.OBJECT_SELF, "TRIGGER_ID", triggerID);
            }

            if (_.GetLocalInt(oPC.Object, triggerID) == 1) return;

            string message = _.GetLocalString(_.OBJECT_SELF, "DISPLAY_TEXT");
            _.SendMessageToPC(oPC.Object, ColorTokenService.Cyan(message));
            _.SetLocalInt(oPC.Object, triggerID, 1);

            _.AssignCommand(oPC.Object, () => _.PlaySound("gui_prompt"));

        }
    }
}
