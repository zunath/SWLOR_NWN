using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Module
{
    public class OnDMAction: IRegisteredEvent
    {
        private readonly IDataService _data;
        private readonly INWNXEvents _nwnxEvents;
        private readonly INWScript _;

        public OnDMAction(
            IDataService data,
            INWNXEvents nwnxEvents,
            INWScript script)
        {
            _data = data;
            _nwnxEvents = nwnxEvents;
            _ = script;
        }

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
            _data.DataQueue.Enqueue(new DatabaseAction(record, DatabaseActionType.Insert));
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
                    string areaName = _nwnxEvents.OnDMSpawnObject_GetArea().Name;
                    NWCreature creature = _nwnxEvents.OnDMSpawnObject_GetObject().Object;
                    int objectTypeID = _nwnxEvents.OnDMSpawnObject_GetObjectType();
                    float x = _nwnxEvents.OnDMSpawnObject_GetPositionX();
                    float y = _nwnxEvents.OnDMSpawnObject_GetPositionY();
                    float z = _nwnxEvents.OnDMSpawnObject_GetPositionZ();
                    creature.SetLocalInt("DM_SPAWNED", NWScript.TRUE);
                    details = areaName + "," + creature.Name + "," + objectTypeID + "," + x + "," + y + "," + z;
                    break;
                case 22: // Give XP
                    amount = _nwnxEvents.OnDMGiveXP_GetAmount();
                    target = _nwnxEvents.OnDMGiveXP_GetTarget();
                    details = amount + "," + target.Name;
                    break;
                case 23: // Give Level
                    amount = _nwnxEvents.OnDMGiveLevels_GetAmount();
                    target = _nwnxEvents.OnDMGiveLevels_GetTarget();
                    details = amount + "," + target.Name;
                    break;
                case 24: // Give Gold
                    amount = _nwnxEvents.OnDMGiveGold_GetAmount();
                    target = _nwnxEvents.OnDMGiveGold_GetTarget();
                    details = amount + "," + target.Name;
                    break;
            }

            return details;
        }


    }
}
