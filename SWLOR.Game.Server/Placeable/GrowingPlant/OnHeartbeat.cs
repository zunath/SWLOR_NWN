using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Placeable.GrowingPlant
{
    public class OnHeartbeat: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable plc = (Object.OBJECT_SELF);
            int growingPlantID = plc.GetLocalInt("GROWING_PLANT_ID");
            if (growingPlantID <= 0) return false;
            
            var growingPlant = DataService.Get<Data.Entity.GrowingPlant>(growingPlantID);
            var plant = DataService.Get<Plant>(growingPlant.PlantID);

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
                plc = (_.CreateObject(_.OBJECT_TYPE_PLACEABLE, plant.Resref, plc.Location));
                plc.SetLocalInt("GROWING_PLANT_ID", growingPlantID);
            }
            
            DataService.SubmitDataChange(growingPlant, DatabaseActionType.Update);
            return true;
        }
    }
}
