using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;

using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;

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
            var area = user.Area;
            var player = new NWPlayer(user);
            var structureID = area.GetLocalString("PC_BASE_STRUCTURE_ID");
            var structureGuid = new Guid(structureID);

            var pcbs = DataService.PCBaseStructure.GetByID(structureGuid);
            var structure = DataService.BaseStructure.GetByID(pcbs.BaseStructureID);
            
            var dbItem = new PCBaseStructureItem
            {
                PCBaseStructureID = pcbs.ID,
                ItemGlobalID = item.GlobalID.ToString(),
                ItemName = item.Name,
                ItemResref = item.Resref,
                ItemTag = item.Tag,
                ItemObject = SerializationService.Serialize(item)
            };

            DataService.SubmitDataChange(dbItem, DatabaseActionType.Insert);
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

        public Animation AnimationID()
        {
            return Animation.LoopingGetMid;
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
            var area = user.Area;

            if (area.GetLocalInt("BUILDING_TYPE") != (int)Enumeration.BuildingType.Starship)
            {
                return "This enhancement may only be deployed inside a starship";
            }

            var structureID = area.GetLocalString("PC_BASE_STRUCTURE_ID");
            var structureGuid = new Guid(structureID);

            var pcbs = DataService.PCBaseStructure.GetByID(structureGuid);
            var structure = DataService.BaseStructure.GetByID(pcbs.BaseStructureID);

            var count = DataService.PCBaseStructureItem.GetNumberOfItemsContainedBy(pcbs.ID) + 1;
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
