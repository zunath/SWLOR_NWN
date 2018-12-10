using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.StructureStorage
{
    public class OnUsed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IBasePermissionService _perm;
        private readonly IDialogService _dialog;
        private readonly IDataService _data;

        public OnUsed(INWScript script,
            IBasePermissionService perm,
            IDialogService dialog,
            IDataService data)
        {
            _ = script;
            _perm = perm;
            _dialog = dialog;
            _data = data;
        }

        public bool Run(params object[] args)
        {
            NWPlayer oPC = (_.GetLastUsedBy());
            NWPlaceable container = (Object.OBJECT_SELF);
            Guid structureID = new Guid(container.GetLocalString("PC_BASE_STRUCTURE_ID"));
            var structure = _data.Get<PCBaseStructure>(structureID);
            
            if (!_perm.HasStructurePermission(oPC, structureID, StructurePermission.CanAccessStructureInventory))
            {
                oPC.FloatingText("You do not have permission to access this structure.");
                return false;
            }

            // Parent structure is a building.
            if (structure.ParentPCBaseStructureID != null)
            {
                var buildingStructure = _data.Get<PCBaseStructure>(structure.ParentPCBaseStructureID);
                var buildingModeType = (StructureModeType)buildingStructure.StructureModeID;

                if (buildingModeType != StructureModeType.Residence)
                {
                    oPC.FloatingText("Persistent storage may only be accessed when the building is in 'Residence' mode.");
                    return false;
                }
            }

            _dialog.StartConversation(oPC, container, "StructureStorage");
            return true;
        }
    }
}
