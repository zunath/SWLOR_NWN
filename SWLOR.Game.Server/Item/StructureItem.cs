using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using System.Linq;


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
            NWPlayer player = NWPlayer.Wrap(user.Object);
            NWArea area = NWArea.Wrap(_.GetAreaFromLocation(targetLocation));
            string sector = _base.GetSectorOfLocation(targetLocation);
            PCBase pcBase = _db.PCBases.Single(x => x.AreaResref == area.Resref && x.Sector == sector);
            var data = _base.GetPlayerTempData(player);
            data.TargetLocation = targetLocation;
            data.TargetArea = area;
            data.PCBaseID = pcBase.PCBaseID;
            data.StructureID = item.GetLocalInt("BASE_STRUCTURE_ID");
            data.StructureItem = item;

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

        public float MaxDistance()
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
