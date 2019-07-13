using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Scripts.Placeable.PlantSeed
{
    public class OnDisturbed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable container = (NWGameObject.OBJECT_SELF);
            NWPlayer oPC = (_.GetLastDisturbed());
            int type = _.GetInventoryDisturbType();
            NWItem item = (_.GetInventoryDisturbItem());
            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(oPC);

            if (type != _.INVENTORY_DISTURB_TYPE_ADDED) return;

            int plantID = item.GetLocalInt("PLANT_ID");

            if (plantID <= 0)
            {
                ItemService.ReturnItem(oPC, item);
                oPC.SendMessage("You cannot plant that item.");
                return;
            }

            Plant plant = FarmingService.GetPlantByID(plantID);
            if (plant == null)
            {
                ItemService.ReturnItem(oPC, item);
                oPC.SendMessage("You cannot plant that item.");
                return;
            }

            int rank = SkillService.GetPCSkillRank(oPC, SkillType.Farming);
            
            if (rank + 2 < plant.Level)
            {
                ItemService.ReturnItem(oPC, item);
                oPC.SendMessage("You do not have enough Farming skill to plant that seed. (Required: " + (plant.Level - 2) + ")");
                return;
            }

            item.Destroy();

            string areaTag = container.Area.Tag;
            Location plantLocation = container.Location;
            int perkBonus = PerkService.GetCreaturePerkLevel(oPC, PerkType.FarmingEfficiency) * 2;
            int ticks = (int)(plant.BaseTicks - ((PerkService.GetCreaturePerkLevel(oPC, PerkType.ExpertFarmer) * 0.05f)) * plant.BaseTicks);
            Data.Entity.GrowingPlant growingPlant = new Data.Entity.GrowingPlant
            {
                PlantID = plant.ID,
                RemainingTicks = ticks,
                LocationAreaTag = areaTag,
                LocationOrientation = _.GetFacingFromLocation(plantLocation),
                LocationX = _.GetPositionFromLocation(plantLocation).m_X,
                LocationY = _.GetPositionFromLocation(plantLocation).m_Y,
                LocationZ = _.GetPositionFromLocation(plantLocation).m_Z,
                IsActive = true,
                DateCreated = DateTime.UtcNow,
                LongevityBonus = perkBonus
            };
            
            DataService.SubmitDataChange(growingPlant, DatabaseActionType.Insert);
            
            NWPlaceable hole = (container.GetLocalObject("FARM_SMALL_HOLE"));
            NWPlaceable plantPlc = (_.CreateObject(_.OBJECT_TYPE_PLACEABLE, "growing_plant", hole.Location));
            plantPlc.SetLocalString("GROWING_PLANT_ID", growingPlant.ID.ToString());
            plantPlc.Name = "Growing Plant (" + plant.Name + ")";

            container.Destroy();
            hole.Destroy();

            int xp = (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(200, plant.Level, rank);

            if (RandomService.Random(100) + 1 <= PerkService.GetCreaturePerkLevel(oPC, PerkType.Lucky) + effectiveStats.Luck)
            {
                xp *= 2;
            }

            SkillService.GiveSkillXP(oPC, SkillType.Farming, xp);
        }
    }
}
