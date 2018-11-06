﻿using System;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Item
{
    public class WaterJug: IActionItem
    {
        private readonly IRandomService _random;
        private readonly IDataService _data;
        private readonly ISkillService _skill;

        public WaterJug(IRandomService random, 
            IDataService data,
            ISkillService skill)
        {
            _random = random;
            _data = data;
            _skill = skill;
        }

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {

            string growingPlantID = target.GetLocalString("GROWING_PLANT_ID");
            if (string.IsNullOrWhiteSpace(growingPlantID))
            {
                user.SendMessage("Water jugs can only target growing plants.");
                return;
            }
            GrowingPlant growingPlant = _data.Single<GrowingPlant>(x => x.ID == new Guid(growingPlantID));
            var plant = _data.Get<Plant>(growingPlant.PlantID);

            if (growingPlant.WaterStatus <= 0)
            {
                user.SendMessage("That plant doesn't need to be watered at this time.");
                return;
            }
            
            if (item.Charges <= 0)
            {
                user.SendMessage("There's no water in that jug!");
                return;
            }
            
            int remainingTicks = growingPlant.RemainingTicks;

            if (growingPlant.WaterStatus > 1)
            {
                remainingTicks = remainingTicks / 2;
            }

            growingPlant.WaterStatus = 0;
            growingPlant.RemainingTicks = remainingTicks;
            _data.SubmitDataChange(growingPlant, DatabaseActionType.Update);

            user.SendMessage("You water the plant.");
            
            int rank = _skill.GetPCSkillRank((NWPlayer)user, SkillType.Farming);
            
            int xp = (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(100, plant.Level, rank);
            _skill.GiveSkillXP((NWPlayer)user, SkillType.Farming, xp);
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return 0;
        }

        public bool FaceTarget()
        {
            return false;
        }

        public int AnimationID()
        {
            return 0;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 0;
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return false;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public bool AllowLocationTarget()
        {
            return false;
        }
    }
}
