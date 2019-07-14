using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;


namespace SWLOR.Game.Server.Service
{
    public static class FarmingService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
        }

        public static void HarvestPlant(NWPlayer player, NWItem shovel, NWPlaceable plant)
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

            GrowingPlant growingPlant = DataService.Single<GrowingPlant>(x => x.ID == growingPlantGuid);
            Plant plantEntity = DataService.Plant.GetByID(growingPlant.PlantID);

            if (string.IsNullOrWhiteSpace(plantEntity.SeedResref))
            {
                player.SendMessage("That plant cannot be harvested.");
                return;
            }
            
            growingPlant.IsActive = false;
            DataService.SubmitDataChange(growingPlant, DatabaseActionType.Update);

            _.CreateItemOnObject(plantEntity.SeedResref, player.Object);
            plant.Destroy();

            player.FloatingText("You harvest the plant.");
        }

        public static string OnModuleExamine(string existingDescription, NWObject examinedObject)
        {
            int plantID = examinedObject.GetLocalInt("PLANT_ID");
            if (plantID <= 0) return existingDescription;
            if (examinedObject.ObjectType != _.OBJECT_TYPE_ITEM) return existingDescription;

            Plant plant = DataService.SingleOrDefault<Plant>(x => x.ID == plantID);
            if (plant == null) return existingDescription;

            existingDescription += ColorTokenService.Orange("This item can be planted. Farming skill required: " + plant.Level) + "\n\n";
            return existingDescription;
        }

        private static void OnModuleLoad()
        {
            List<GrowingPlant> plants = DataService.Where<GrowingPlant>(x => x.IsActive).ToList();

            foreach (GrowingPlant growingPlant in plants)
            {
                var plant = DataService.Plant.GetByID(growingPlant.PlantID);
                string resref = "growing_plant";
                if (growingPlant.RemainingTicks <= 0)
                    resref = plant.Resref;

                NWArea area = (_.GetObjectByTag(growingPlant.LocationAreaTag));
                Vector position = _.Vector((float)growingPlant.LocationX, (float)growingPlant.LocationY, (float)growingPlant.LocationZ);
                Location location = _.Location(area.Object, position, (float)growingPlant.LocationOrientation);
                NWPlaceable plantPlc = (_.CreateObject(_.OBJECT_TYPE_PLACEABLE, resref, location));
                plantPlc.SetLocalString("GROWING_PLANT_ID", growingPlant.ID.ToString());

                if (growingPlant.RemainingTicks > 0)
                {
                    plantPlc.Name = "Growing Plant (" + plant.Name + ")";
                }
            }
        }
        
        public static void RemoveGrowingPlant(NWPlaceable plant)
        {
            string growingPlantID = plant.GetLocalString("GROWING_PLANT_ID");
            Guid? growingPlantGuid = null;
            if(!string.IsNullOrWhiteSpace(growingPlantID))
                growingPlantGuid = new Guid(growingPlantID);

            if (growingPlantGuid == null) return;

            GrowingPlant growingPlant = DataService.Single<GrowingPlant>(x => x.ID == growingPlantGuid);
            growingPlant.IsActive = false;
            DataService.SubmitDataChange(growingPlant, DatabaseActionType.Update);
        }

        public static GrowingPlant GetGrowingPlantByID(Guid growingPlantID)
        {
            return DataService.Single<GrowingPlant>(x => x.ID == growingPlantID);
        }

        public static Plant GetPlantByID(int plantID)
        {
            return DataService.Single<Plant>(x => x.ID == plantID);
        }

    }
}
