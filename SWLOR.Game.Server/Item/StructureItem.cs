using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using System.Linq;
using BuildingType = SWLOR.Game.Server.Enumeration.BuildingType;


namespace SWLOR.Game.Server.Item
{
    public class StructureItem : IActionItem
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly IDialogService _dialog;
        private readonly IBaseService _base;

        public StructureItem(
            INWScript script,
            IDataContext db,
            IDialogService dialog,
            IBaseService @base)
        {
            _ = script;
            _db = db;
            _dialog = dialog;
            _base = @base;
        }

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
            int parentStructureID = area.GetLocalInt("PC_BASE_STRUCTURE_ID");
            int pcBaseID = area.GetLocalInt("PC_BASE_ID");
            var data = _base.GetPlayerTempData(player);
            data.TargetLocation = targetLocation;
            data.TargetArea = area;
            data.StructureID = item.GetLocalInt("BASE_STRUCTURE_ID");
            data.StructureItem = item;

            // Structure is being placed inside an apartment.
            if (pcBaseID > 0)
            {
                data.PCBaseID = pcBaseID;
                data.ParentStructureID = null;
                data.BuildingType = BuildingType.Apartment;
            }
            // Structure is being placed inside a building.
            else if (parentStructureID > 0)
            {
                var parentStructure = _db.PCBaseStructures.Single(x => x.PCBaseStructureID == parentStructureID);
                data.PCBaseID = parentStructure.PCBaseID;
                data.ParentStructureID = parentStructureID;
                data.BuildingType = BuildingType.Interior;
            }
            // Structure is being placed outside of a building.
            else
            {
                string sector = _base.GetSectorOfLocation(targetLocation);
                PCBase pcBase = _db.PCBases.Single(x => x.AreaResref == area.Resref && x.Sector == sector);
                data.PCBaseID = pcBase.PCBaseID;
                data.ParentStructureID = null;
                data.BuildingType = BuildingType.Exterior;
            }
            
            _dialog.StartConversation(user, user, "PlaceStructure");
        }

        public bool FaceTarget()
        {
            return false;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            int structureID = item.GetLocalInt("BASE_STRUCTURE_ID");
            return target.IsValid ? 
                "You must select an empty location to use that item." : 
                _base.CanPlaceStructure(user, item, targetLocation, structureID);
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 5.0f;
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
