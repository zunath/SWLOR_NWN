using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Module
{
    public class OnDMAction: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            int actionTypeID = Convert.ToInt32(args[0]);
            string details = ProcessEventAndBuildDetails(actionTypeID);
            
            NWObject dm = Object.OBJECT_SELF;
            
            var record = new DMAction
            {
                DMActionTypeID = actionTypeID,
                Name = dm.Name,
                CDKey = _.GetPCPublicCDKey(dm),
                DateOfAction = DateTime.UtcNow,
                Details = details
            };

            // Don't cache DM actions.
            DataService.DataQueue.Enqueue(new DatabaseAction(record, DatabaseActionType.Insert));
            return true;
        }

        private string ProcessEventAndBuildDetails(int eventID)
        {
            string details = string.Empty;
            NWObject target;
            int amount;

            switch (eventID)
            {
                case 1: // Spawn Creature
                    string areaName = NWNXEvents.OnDMSpawnObject_GetArea().Name;
                    NWCreature creature = NWNXEvents.OnDMSpawnObject_GetObject().Object;
                    int objectTypeID = NWNXEvents.OnDMSpawnObject_GetObjectType();
                    float x = NWNXEvents.OnDMSpawnObject_GetPositionX();
                    float y = NWNXEvents.OnDMSpawnObject_GetPositionY();
                    float z = NWNXEvents.OnDMSpawnObject_GetPositionZ();
                    creature.SetLocalInt("DM_SPAWNED", _.TRUE);
                    details = areaName + "," + creature.Name + "," + objectTypeID + "," + x + "," + y + "," + z;
                    break;
                case 22: // Give XP
                    amount = NWNXEvents.OnDMGiveXP_GetAmount();
                    target = NWNXEvents.OnDMGiveXP_GetTarget();
                    details = amount + "," + target.Name;
                    break;
                case 23: // Give Level
                    amount = NWNXEvents.OnDMGiveLevels_GetAmount();
                    target = NWNXEvents.OnDMGiveLevels_GetTarget();
                    details = amount + "," + target.Name;
                    break;
                case 24: // Give Gold
                    amount = NWNXEvents.OnDMGiveGold_GetAmount();
                    target = NWNXEvents.OnDMGiveGold_GetTarget();
                    details = amount + "," + target.Name;
                    break;
            }

            return details;
        }


    }
}
