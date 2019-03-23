using SWLOR.Game.Server.GameObject;

using NWN;

using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;
using SWLOR.Game.Server.Enumeration;
using System;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Event.Module
{
    public class OnModuleExamine: IRegisteredEvent
    {
        
        private readonly IFarmingService _farming;
        private readonly IDurabilityService _durability;
        
        private readonly IExaminationService _examination;
        private readonly IModService _mod;
        
        public OnModuleExamine(
            
            IFarmingService farming,
            IDurabilityService durability,
            
            
            IExaminationService examination,
            IModService mod
            )
        {
            
            _farming = farming;
            _durability = durability;
            
            _examination = examination;
            _mod = mod;
            
        }

        public bool Run(params object[] args)
        {
            NWPlayer examiner = (Object.OBJECT_SELF);
            NWObject examinedObject = NWNXEvents.OnExamineObject_GetTarget();
            if (_examination.OnModuleExamine(examiner, examinedObject)) return true;

            string description = _.GetDescription(examinedObject.Object, _.TRUE) + "\n\n";

            if (examinedObject.IsCreature)
            {
                int racialID = Convert.ToInt32(_.Get2DAString("racialtypes", "Name", _.GetRacialType(examinedObject)));
                string racialtype = _.GetStringByStrRef(racialID);
                description += ColorTokenService.Green("Racial Type: ") + racialtype;
            }

            description = _mod.OnModuleExamine(description, examiner, examinedObject);
            description = ItemService.OnModuleExamine(description, examiner, examinedObject);
            description = _durability.OnModuleExamine(description, examinedObject);
            description = _farming.OnModuleExamine(description, examinedObject);
            
            if (string.IsNullOrWhiteSpace(description)) return false;
            _.SetDescription(examinedObject.Object, description, _.FALSE);
            _.SetDescription(examinedObject.Object, description);
            

            return true;
        }
    }
}
