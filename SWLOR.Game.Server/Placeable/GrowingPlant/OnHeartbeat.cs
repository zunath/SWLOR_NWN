using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;

namespace SWLOR.Game.Server.Placeable.GrowingPlant
{
    public class OnHeartbeat: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataContext _db;

        public OnHeartbeat(INWScript script,
            IDataContext db)
        {
            _ = script;
            _db = db;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable plant = (Object.OBJECT_SELF);
            int growingPlantID = plant.GetLocalInt("GROWING_PLANT_ID");
            if (growingPlantID <= 0) return false;
            
            Data.Entity.GrowingPlant growingPlant = _db.GrowingPlants.Single(x => x.GrowingPlantID == growingPlantID);
            growingPlant.RemainingTicks--;
            growingPlant.TotalTicks++;

            int waterTicks = growingPlant.Plant.WaterTicks;
            if (waterTicks > 0 && growingPlant.TotalTicks % waterTicks == 0)
            {
                int maxWaterStatus = growingPlant.Plant.BaseTicks / growingPlant.Plant.WaterTicks;

                if (growingPlant.WaterStatus < maxWaterStatus)
                {
                    growingPlant.WaterStatus++;
                    growingPlant.RemainingTicks = growingPlant.RemainingTicks * growingPlant.WaterStatus;
                }
            }

            if (growingPlant.RemainingTicks <= 0)
            {
                plant.Destroy();
                plant = (_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, growingPlant.Plant.Resref, plant.Location));
                plant.SetLocalInt("GROWING_PLANT_ID", growingPlantID);
            }
            
            _db.SaveChanges();
            return true;
        }
    }
}
