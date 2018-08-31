using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class FarmingService: IFarmingService
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly IRandomService _random;
        private readonly IColorTokenService _color;

        public FarmingService(INWScript script, 
            IDataContext db,
            IRandomService random,
            IColorTokenService color)
        {
            _ = script;
            _db = db;
            _random = random;
            _color = color;
        }

        public void HarvestPlant(NWPlayer player, NWItem shovel, NWPlaceable plant)
        {
            int growingPlantID = plant.GetLocalInt("GROWING_PLANT_ID");
            if (growingPlantID <= 0) return;

            int charges = shovel.Charges;
            if (charges <= 0)
            {
                player.SendMessage("Your shovel is broken.");
                return;
            }

            GrowingPlant growingPlant = _db.GrowingPlants.Single(x => x.GrowingPlantID == growingPlantID);
            Plant plantEntity = growingPlant.Plant;

            if (string.IsNullOrWhiteSpace(plantEntity.SeedResref))
            {
                player.SendMessage("That plant cannot be harvested.");
                return;
            }

            PlayerCharacter pcEntity = _db.PlayerCharacters.Single(x => x.PlayerID == player.GlobalID);

            // Farmers get a 5% chance to not expend a charge.
            if (pcEntity.BackgroundID != (int)BackgroundType.Farmer || _random.Random(100) + 1 > 5 )
            {
                shovel.ReduceCharges();
            }

            growingPlant.IsActive = false;
            _db.SaveChanges();

            _.CreateItemOnObject(plantEntity.SeedResref, player.Object);
            plant.Destroy();

            player.FloatingText("You harvest the plant.");
        }

        public string OnModuleExamine(string existingDescription, NWObject examinedObject)
        {
            int plantID = examinedObject.GetLocalInt("PLANT_ID");
            if (plantID <= 0) return existingDescription;
            if (examinedObject.ObjectType != NWScript.OBJECT_TYPE_ITEM) return existingDescription;

            Plant plant = _db.Plants.SingleOrDefault(x => x.PlantID == plantID);
            if (plant == null) return existingDescription;

            existingDescription += _color.Orange("This item can be planted. Farming skill required: " + plant.Level) + "\n\n";
            return existingDescription;
        }

        public void OnModuleLoad()
        {
            List<GrowingPlant> plants = _db.GrowingPlants.Where(x => x.IsActive).ToList();

            foreach (GrowingPlant plant in plants)
            {
                string resref = "growing_plant";
                if (plant.RemainingTicks <= 0)
                    resref = plant.Plant.Resref;

                NWArea area = NWArea.Wrap(_.GetObjectByTag(plant.LocationAreaTag));
                Vector position = _.Vector(plant.LocationX, plant.LocationY, plant.LocationZ);
                Location location = _.Location(area.Object, position, plant.LocationOrientation);
                NWPlaceable plantPlc = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, resref, location));
                plantPlc.SetLocalInt("GROWING_PLANT_ID", plant.GrowingPlantID);

                if (plant.RemainingTicks > 0)
                {
                    plantPlc.Name = "Growing Plant (" + plant.Plant.Name + ")";
                }
            }
        }
        
        public void RemoveGrowingPlant(NWPlaceable plant)
        {
            int growingPlantID = plant.GetLocalInt("GROWING_PLANT_ID");
            if (growingPlantID <= 0) return;

            GrowingPlant growingPlant = _db.GrowingPlants.Single(x => x.GrowingPlantID == growingPlantID);
            growingPlant.IsActive = false;
            _db.SaveChanges();
        }

        public GrowingPlant GetGrowingPlantByID(int growingPlantID)
        {
            return _db.GrowingPlants.Single(x => x.GrowingPlantID == growingPlantID);
        }

        public Plant GetPlantByID(int plantID)
        {
            return _db.Plants.Single(x => x.PlantID == plantID);
        }

    }
}
