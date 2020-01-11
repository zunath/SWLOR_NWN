﻿using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;

using SWLOR.Game.Server.ValueObject;
using System.Linq;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;
using static NWN._;

namespace SWLOR.Game.Server.Item
{
    public class SSEnhancement : IActionItem
    {
        public string CustomKey => null;

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWArea area = user.Area;
            NWPlayer player = new NWPlayer(user);
            string structureID = area.GetLocalString("PC_BASE_STRUCTURE_ID");
            Guid structureGuid = new Guid(structureID);

            PCBaseStructure pcbs = DataService.PCBaseStructure.GetByID(structureGuid);
            
            var dbItem = new PCBaseStructureItem
            {
                ItemName = item.Name,
                ItemResref = item.Resref,
                ItemTag = item.Tag,
                ItemObject = SerializationService.Serialize(item)
            };
            pcbs.Items[structureGuid] = dbItem;

            DataService.Set(pcbs);
            player.SendMessage(item.Name + " was successfully added to your ship.  Access the cargo bay via the ship's computer to remove it.");
            item.Destroy();
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return 6.0f;
        }

        public bool FaceTarget()
        {
            return false;
        }

        public Animation AnimationType()
        {
            return Animation.Get_Mid;
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
            NWArea area = user.Area;

            if (area.GetLocalInt("BUILDING_TYPE") != (int)Enumeration.BuildingType.Starship)
            {
                return "This enhancement may only be deployed inside a starship";
            }

            string structureID = area.GetLocalString("PC_BASE_STRUCTURE_ID");
            Guid structureGuid = new Guid(structureID);

            PCBaseStructure pcbs = DataService.PCBaseStructure.GetByID(structureGuid);
            var structure = BaseService.GetBaseStructure(pcbs.BaseStructureID);

            int count = pcbs.Items.Count + 1;
            if (count > (structure.ResourceStorage + pcbs.StructureBonus))
            {
                return "Your cargo bay is full!  You cannot add any enhancements.";
            }

            return "";
        }

        public bool AllowLocationTarget()
        {
            return true;
        }
    }
}
