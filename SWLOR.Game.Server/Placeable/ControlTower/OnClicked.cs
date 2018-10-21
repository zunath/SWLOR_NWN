using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.ControlTower
{
    public class OnClicked: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDialogService _dialog;
        private readonly IBasePermissionService _perm;
        private readonly IDataContext _db;

        public OnClicked(
            INWScript script,
            IDialogService dialog,
            IBasePermissionService perm,
            IDataContext db)
        {
            _ = script;
            _dialog = dialog;
            _perm = perm;
            _db = db;
        }

        public bool Run(params object[] args)
        {
            NWPlayer clicker = (_.GetPlaceableLastClickedBy());
            NWPlaceable tower = (Object.OBJECT_SELF);

            clicker.ClearAllActions();
            if (!clicker.IsPlayer) return false;
            if (_.GetDistanceBetween(clicker.Object, tower.Object) > 5.0f)
            {
                clicker.SendMessage("You are too far away to interact with that control tower.");
                return false;
            }
            int structureID = tower.GetLocalInt("PC_BASE_STRUCTURE_ID");
            PCBaseStructure structure = _db.PCBaseStructures.Single(x => x.PCBaseStructureID == structureID);

            if (_perm.HasBasePermission(clicker, structure.PCBaseID, BasePermission.CanManageBaseFuel))
            {
                _dialog.StartConversation(clicker, tower, "ControlTower");
            }
            else
            {
                clicker.SendMessage("You don't have permission to interact with this control tower.");
            }

            return true;
        }
    }
}
