using System;
using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Trigger
{
    public class ExplorationTrigger: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWCreature oPC = (_.GetEnteringObject());
            if (!oPC.IsPlayer) return false;

            string triggerID = _.GetLocalString(Object.OBJECT_SELF, "TRIGGER_ID");
            if (string.IsNullOrWhiteSpace(triggerID))
            {
                triggerID = Guid.NewGuid().ToString();
                _.SetLocalString(Object.OBJECT_SELF, "TRIGGER_ID", triggerID);
            }

            if (_.GetLocalInt(oPC.Object, triggerID) == 1) return false;

            string message = _.GetLocalString(Object.OBJECT_SELF, "DISPLAY_TEXT");
            _.SendMessageToPC(oPC.Object, ColorTokenService.Cyan(message));
            _.SetLocalInt(oPC.Object, triggerID, 1);

            _.AssignCommand(oPC.Object, () => _.PlaySound("gui_prompt"));

            return true;
        }
    }
}
