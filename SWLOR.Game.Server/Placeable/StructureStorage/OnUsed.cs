using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.StructureStorage
{
    public class OnUsed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IBasePermissionService _perm;
        private readonly IDialogService _dialog;

        public OnUsed(INWScript script,
            IBasePermissionService perm,
            IDialogService dialog)
        {
            _ = script;
            _perm = perm;
            _dialog = dialog;
        }

        public bool Run(params object[] args)
        {
            NWPlayer oPC = (_.GetLastUsedBy());
            NWPlaceable container = (Object.OBJECT_SELF);
            int structureID = container.GetLocalInt("PC_BASE_STRUCTURE_ID");

            if (!_perm.HasStructurePermission(oPC, structureID, StructurePermission.CanAccessStructureInventory))
            {
                oPC.FloatingText("You do not have permission to access this structure.");
                return false;
            }

            _dialog.StartConversation(oPC, container, "StructureStorage");
            return true;
        }
    }
}
