using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class FarmingService: IFarmingService
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly IRandomService _random;
        private readonly IColorTokenService _color;

        public FarmingService(INWScript script, 
            IDataService data,
            IRandomService random,
            IColorTokenService color)
        {
            _ = script;
            _data = data;
            _random = random;
            _color = color;
        }

        public void HarvestPlant(NWPlayer player, NWItem shovel, NWPlaceable plant)
        {
            string growingPlantID = plant.GetLocalString("GROWING_PLANT_ID");
            Guid? growingPlantGuid = null;
            if (!string.IsNullOrWhiteSpace(growingPlantID))
                growingPlantGuid = new Guid(growingPlantID);

            if (growingPlantGuid == null) return;

            int charges = shovel.Charges;
            if (charges <= 0)
            {
                player.SendMessage("Your shovel is broken.");
                return;
            }

            GrowingPlant growingPlant = _data.Single<GrowingPlant>(x => x.ID == growingPlantGuid);
            Plant plantEntity = _data.Get<Plant>(growingPlant.PlantID);

            if (string.IsNullOrWhiteSpace(plantEntity.SeedResref))
            {
                player.SendMessage("That plant cannot be harvested.");
                return;
            }
            
            growingPlant.IsActive = false;
            _data.SubmitDataChange(growingPlant, DatabaseActionType.Update);

            _.CreateItemOnObject(plantEntity.SeedResref, player.Object);
            plant.Destroy();

            player.FloatingText("You harvest the plant.");
        }

        public string OnModuleExamine(string existingDescription, NWObject examinedObject)
        {
            int plantID = examinedObject.GetLocalInt("PLANT_ID");
            if (plantID <= 0) return existingDescription;
            if (examinedObject.ObjectType != NWScript.OBJECT_TYPE_ITEM) return existingDescription;

            Plant plant = _data.SingleOrDefault<Plant>(x => x.ID == plantID);
            if (plant == null) return existingDescription;

            existingDescription += _color.Orange("This item can be planted. Farming skill required: " + plant.Level) + "\n\n";
            return existingDescription;
        }

        public void OnModuleLoad()
        {
            List<GrowingPlant> plants = _data.Where<GrowingPlant>(x => x.IsActive).ToList();

            foreach (GrowingPlant growingPlant in plants)
            {
                var plant = _data.Get<Plant>(growingPlant.PlantID);
                string resref = "growing_plant";
                if (growingPlant.RemainingTicks <= 0)
                    resref = plant.Resref;

                NWArea area = (_.GetObjectByTag(growingPlant.LocationAreaTag));
                Vector position = _.Vector((float)growingPlant.LocationX, (float)growingPlant.LocationY, (float)growingPlant.LocationZ);
                Location location = _.Location(area.Object, position, (float)growingPlant.LocationOrientation);
                NWPlaceable plantPlc = (_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, resref, location));
                plantPlc.SetLocalString("GROWING_PLANT_ID", growingPlant.ID.ToString());

                if (growingPlant.RemainingTicks > 0)
                {
                    plantPlc.Name = "Growing Plant (" + plant.Name + ")";
                }
            }
        }
        
        public void RemoveGrowingPlant(NWPlaceable plant)
        {
            string growingPlantID = plant.GetLocalString("GROWING_PLANT_ID");
            Guid? growingPlantGuid = null;
            if(!string.IsNullOrWhiteSpace(growingPlantID))
                growingPlantGuid = new Guid(growingPlantID);

            if (growingPlantID == null) return;

            GrowingPlant growingPlant = _data.Single<GrowingPlant>(x => x.ID == growingPlantGuid);
            growingPlant.IsActive = false;
            _data.SubmitDataChange(growingPlant, DatabaseActionType.Update);
        }

        public GrowingPlant GetGrowingPlantByID(Guid growingPlantID)
        {
            return _data.Single<GrowingPlant>(x => x.ID == growingPlantID);
        }

        public Plant GetPlantByID(int plantID)
        {
            return _data.Single<Plant>(x => x.ID == plantID);
        }

    }
}
