using System;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.GrowingPlant
{
    public class OnUsed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IFarmingService _farming;

        public OnUsed(INWScript script, IFarmingService farming)
        {
            _ = script;
            _farming = farming;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable plant = (Object.OBJECT_SELF);
            string growingPlantID = plant.GetLocalString("GROWING_PLANT_ID");
            if (string.IsNullOrWhiteSpace(growingPlantID)) return false;

            NWPlayer oPC = (_.GetLastUsedBy());
            Data.Entity.GrowingPlant growingPlant = _farming.GetGrowingPlantByID(new Guid(growingPlantID));
            if (growingPlant.WaterStatus <= 0)
            {
                oPC.SendMessage("This plant doesn't seem to need anything right now.");
                return true;
            }

            oPC.SendMessage("This plant needs to be watered. Use a Water Jug on it to water it. These can be crafted with the Metalworking skill.");
            return true;
        }
    }
}
