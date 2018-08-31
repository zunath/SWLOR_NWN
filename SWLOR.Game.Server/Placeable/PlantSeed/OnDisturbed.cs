using System;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;
using Object = SWLOR.Game.Server.NWN.NWScript.Object;

namespace SWLOR.Game.Server.Placeable.PlantSeed
{
    public class OnDisturbed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IItemService _item;
        private readonly IDataContext _db;
        private readonly IRandomService _random;
        private readonly ISkillService _skill;
        private readonly IPerkService _perk;
        private readonly IFarmingService _farming;

        public OnDisturbed(INWScript script,
            IItemService item,
            IDataContext db,
            IRandomService random,
            ISkillService skill,
            IPerkService perk,
            IFarmingService farming)
        {
            _ = script;
            _item = item;
            _db = db;
            _random = random;
            _skill = skill;
            _perk = perk;
            _farming = farming;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable container = NWPlaceable.Wrap(Object.OBJECT_SELF);
            NWPlayer oPC = NWPlayer.Wrap(_.GetLastDisturbed());
            int type = _.GetInventoryDisturbType();
            NWItem item = NWItem.Wrap(_.GetInventoryDisturbItem());

            if (type != NWScript.INVENTORY_DISTURB_TYPE_ADDED) return false;

            int plantID = item.GetLocalInt("PLANT_ID");

            if (plantID <= 0)
            {
                _item.ReturnItem(oPC, item);
                oPC.SendMessage("You cannot plant that item.");
                return true;
            }

            Plant plant = _farming.GetPlantByID(plantID);
            if (plant == null)
            {
                _item.ReturnItem(oPC, item);
                oPC.SendMessage("You cannot plant that item.");
                return true;
            }

            PCSkill pcSkill = _skill.GetPCSkill(oPC, SkillType.Farming);
            int rank = 0;
            if (pcSkill != null)
            {
                rank = pcSkill.Rank;
            }

            if (rank + 2 < plant.Level)
            {
                _item.ReturnItem(oPC, item);
                oPC.SendMessage("You do not have enough Farming skill to plant that seed. (Required: " + (plant.Level - 2) + ")");
                return true;
            }

            item.Destroy();

            string areaTag = container.Area.Tag;
            Location plantLocation = container.Location;
            int perkBonus = _perk.GetPCPerkLevel(oPC, PerkType.FarmingEfficiency) * 2;
            int ticks = (int)(plant.BaseTicks - ((_perk.GetPCPerkLevel(oPC, PerkType.ExpertFarmer) * 0.05f)) * plant.BaseTicks);
            Data.Entities.GrowingPlant growingPlant = new Data.Entities.GrowingPlant();
            growingPlant.PlantID = plant.PlantID;
            growingPlant.RemainingTicks = ticks;
            growingPlant.LocationAreaTag = areaTag;
            growingPlant.LocationOrientation = _.GetFacingFromLocation(plantLocation);
            growingPlant.LocationX = _.GetPositionFromLocation(plantLocation).m_X;
            growingPlant.LocationY = _.GetPositionFromLocation(plantLocation).m_Y;
            growingPlant.LocationZ = _.GetPositionFromLocation(plantLocation).m_Z;
            growingPlant.IsActive = true;
            growingPlant.DateCreated = DateTime.UtcNow; 
            growingPlant.LongevityBonus = perkBonus;

            _db.GrowingPlants.Add(growingPlant);
            _db.SaveChanges();
            
            NWPlaceable hole = NWPlaceable.Wrap(container.GetLocalObject("FARM_SMALL_HOLE"));
            NWPlaceable plantPlc = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "growing_plant", hole.Location));
            plantPlc.SetLocalInt("GROWING_PLANT_ID", growingPlant.GrowingPlantID);
            plantPlc.Name = "Growing Plant (" + plant.Name + ")";

            container.Destroy();
            hole.Destroy();

            int xp = (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(200, plant.Level, rank);

            if (_random.Random(100) + 1 <= _perk.GetPCPerkLevel(oPC, PerkType.Lucky) + oPC.EffectiveLuckBonus)
            {
                xp *= 2;
            }

            _skill.GiveSkillXP(oPC, SkillType.Farming, xp);
            return true;
        }
    }
}
