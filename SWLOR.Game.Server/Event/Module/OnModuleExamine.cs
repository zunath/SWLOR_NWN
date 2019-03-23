using SWLOR.Game.Server.GameObject;

using NWN;


using Object = NWN.Object;
using System;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Event.Module
{
    public class OnModuleExamine: IRegisteredEvent
    {

        public bool Run(params object[] args)
        {
            NWPlayer examiner = (Object.OBJECT_SELF);
            NWObject examinedObject = NWNXEvents.OnExamineObject_GetTarget();
            if (ExaminationService.OnModuleExamine(examiner, examinedObject)) return true;

            string description = _.GetDescription(examinedObject.Object, _.TRUE) + "\n\n";

            if (examinedObject.IsCreature)
            {
                int racialID = Convert.ToInt32(_.Get2DAString("racialtypes", "Name", _.GetRacialType(examinedObject)));
                string racialtype = _.GetStringByStrRef(racialID);
                description += ColorTokenService.Green("Racial Type: ") + racialtype;
            }

            description = ModService.OnModuleExamine(description, examiner, examinedObject);
            description = ItemService.OnModuleExamine(description, examiner, examinedObject);
            description = DurabilityService.OnModuleExamine(description, examinedObject);
            description = FarmingService.OnModuleExamine(description, examinedObject);
            
            if (string.IsNullOrWhiteSpace(description)) return false;
            _.SetDescription(examinedObject.Object, description, _.FALSE);
            _.SetDescription(examinedObject.Object, description);
            

            return true;
        }
    }
}
