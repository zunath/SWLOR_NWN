﻿using System;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.PlantSeed
{
    public class OnDisturbed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IItemService _item;
        private readonly IDataService _data;
        private readonly IRandomService _random;
        private readonly ISkillService _skill;
        private readonly IPerkService _perk;
        private readonly IFarmingService _farming;
        private readonly IPlayerStatService _playerStat;

        public OnDisturbed(INWScript script,
            IItemService item,
            IDataService data,
            IRandomService random,
            ISkillService skill,
            IPerkService perk,
            IFarmingService farming,
            IPlayerStatService playerStat)
        {
            _ = script;
            _item = item;
            _data = data;
            _random = random;
            _skill = skill;
            _perk = perk;
            _farming = farming;
            _playerStat = playerStat;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable container = (Object.OBJECT_SELF);
            NWPlayer oPC = (_.GetLastDisturbed());
            int type = _.GetInventoryDisturbType();
            NWItem item = (_.GetInventoryDisturbItem());
            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(oPC);

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

            int rank = _skill.GetPCSkillRank(oPC, SkillType.Farming);
            
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
            
            _data.SubmitDataChange(growingPlant, DatabaseActionType.Insert);
            
            NWPlaceable hole = (container.GetLocalObject("FARM_SMALL_HOLE"));
            NWPlaceable plantPlc = (_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "growing_plant", hole.Location));
            plantPlc.SetLocalString("GROWING_PLANT_ID", growingPlant.ID.ToString());
            plantPlc.Name = "Growing Plant (" + plant.Name + ")";

            container.Destroy();
            hole.Destroy();

            int xp = (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(200, plant.Level, rank);

            if (_random.Random(100) + 1 <= _perk.GetPCPerkLevel(oPC, PerkType.Lucky) + effectiveStats.Luck)
            {
                xp *= 2;
            }

            _skill.GiveSkillXP(oPC, SkillType.Farming, xp);
            return true;
        }
    }
}
