using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.GrowingPlant
{
    public class OnHeartbeat: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataService _data;

        public OnHeartbeat(INWScript script,
            IDataService data)
        {
            _ = script;
            _data = data;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable plc = (Object.OBJECT_SELF);
            int growingPlantID = plc.GetLocalInt("GROWING_PLANT_ID");
            if (growingPlantID <= 0) return false;
            
            var growingPlant = _data.Get<Data.Entity.GrowingPlant>(growingPlantID);
            var plant = _data.Get<Plant>(growingPlant.PlantID);

            growingPlant.RemainingTicks--;
            growingPlant.TotalTicks++;

            int waterTicks = plant.WaterTicks;
            if (waterTicks > 0 && growingPlant.TotalTicks % waterTicks == 0)
            {
                int maxWaterStatus = plant.BaseTicks / plant.WaterTicks;

                if (growingPlant.WaterStatus < maxWaterStatus)
                {
                    growingPlant.WaterStatus++;
                    growingPlant.RemainingTicks = growingPlant.RemainingTicks * growingPlant.WaterStatus;
                }
            }

            if (growingPlant.RemainingTicks <= 0)
            {
                plc.Destroy();
                plc = (_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, plant.Resref, plc.Location));
                plc.SetLocalInt("GROWING_PLANT_ID", growingPlantID);
            }
            
            _data.SubmitDataChange(growingPlant, DatabaseActionType.Update);
            return true;
        }
    }
}
