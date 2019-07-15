using System;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;

using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service;
using BuildingType = SWLOR.Game.Server.Enumeration.BuildingType;


namespace SWLOR.Game.Server.Item
{
    public class StructureItem : IActionItem
    {
        public string CustomKey => null;

        public bool AllowLocationTarget()
        {
            return true;
        }

        public int AnimationID()
        {
            return 0;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = (user.Object);
            NWArea area = (_.GetAreaFromLocation(targetLocation));
            string parentStructureID = area.GetLocalString("PC_BASE_STRUCTURE_ID");
            string pcBaseID = area.GetLocalString("PC_BASE_ID");
            var data = BaseService.GetPlayerTempData(player);
            data.TargetLocation = targetLocation;
            data.TargetArea = area;
            data.BaseStructureID = item.GetLocalInt("BASE_STRUCTURE_ID");
            data.StructureItem = item;

            // Structure is being placed inside an apartment.
            if (!string.IsNullOrWhiteSpace(pcBaseID))
            {
                data.PCBaseID = new Guid(pcBaseID);
                data.ParentStructureID = null;
                data.BuildingType = BuildingType.Apartment;
            }
            // Structure is being placed inside a building or starship.
            else if (!string.IsNullOrWhiteSpace(parentStructureID))
            {
                var parentStructureGuid = new Guid(parentStructureID);
                var parentStructure = DataService.PCBaseStructure.GetByID(parentStructureGuid);
                data.PCBaseID = parentStructure.PCBaseID;
                data.ParentStructureID = parentStructureGuid;

                if (area.GetLocalInt("BUILDING_TYPE") == (int) BuildingType.Starship)
                {
                    data.BuildingType = BuildingType.Starship;
                }
                else
                {
                    data.BuildingType = BuildingType.Interior;
                }
            }
            // Structure is being placed outside of a building.
            else
            {
                string sector = BaseService.GetSectorOfLocation(targetLocation);
                PCBase pcBase = DataService.PCBase.GetByAreaResrefAndSector(area.Resref, sector);
                data.PCBaseID = pcBase.ID;
                data.ParentStructureID = null;
                data.BuildingType = BuildingType.Exterior;
            }

            DialogService.StartConversation(user, user, "PlaceStructure");
        }

        public bool FaceTarget()
        {
            return false;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            int structureID = item.GetLocalInt("BASE_STRUCTURE_ID");

            // Intercept here to handle control tower upgrades.
            string upgrade = BaseService.UpgradeControlTower(user, item, target);
            if (upgrade != "")
            {
                return upgrade;
            }
            else if (target.IsValid)
            {
                return "You must select an empty location to use that item.";
            }
            else
            {
                return BaseService.CanPlaceStructure(user, item, targetLocation, structureID);
            }
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 10.0f;
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return false;
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return 0.5f;
        }

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }
    }
}
