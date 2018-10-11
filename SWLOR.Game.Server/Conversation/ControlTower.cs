using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Conversation
{
    public class ControlTower: ConversationBase
    {
        private readonly IDataContext _db;
        private readonly IBasePermissionService _perm;
        private readonly ISerializationService _serialization;

        public ControlTower(
            INWScript script, 
            IDialogService dialog,
            IDataContext db,
            IBasePermissionService perm,
            ISerializationService serialization) 
            : base(script, dialog)
        {
            _db = db;
            _perm = perm;
            _serialization = serialization;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "What would you like to do with this control tower?",
                "Access Fuel Bay",
                "Access Stronidium Bay",
                "Access Resource Bay");

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }
        
        public override void Initialize()
        {
            int structureID = GetDialogTarget().GetLocalInt("PC_BASE_STRUCTURE_ID");
            PCBaseStructure structure = _db.PCBaseStructures.Single(x => x.PCBaseStructureID == structureID);

            if (!_perm.HasBasePermission(GetPC(), structure.PCBaseID, BasePermission.CanManageBaseFuel))
            {
                SetResponseVisible("MainPage", 1, false);
                SetResponseVisible("MainPage", 2, false);
            }

            if (!_perm.HasBasePermission(GetPC(), structure.PCBaseID, BasePermission.CanAccessStructureInventory))
            {
                SetResponseVisible("MainPage", 3, false);
            }
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (responseID)
            {
                case 1: // Access Fuel Bay
                    OpenFuelBay(false);
                    break;
                case 2: // Access Stronidium Bay
                    OpenFuelBay(true);
                    break;
                case 3: // Access Resource Bay
                    OpenResourceBay();
                    break;
            }

        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }


        private void OpenFuelBay(bool isStronidium)
        {
            NWPlaceable tower = (NWPlaceable)GetDialogTarget();
            NWPlayer oPC = GetPC();

            if (((NWPlaceable)tower.GetLocalObject("CONTROL_TOWER_FUEL_BAY")).IsValid)
            {
                oPC.FloatingText("Someone else is already accessing that structure's inventory. Please wait.");
                return;
            }

            int structureID = tower.GetLocalInt("PC_BASE_STRUCTURE_ID");
            var structure = _db.PCBaseStructures.Single(x => x.PCBaseStructureID == structureID);
            Location location = oPC.Location;
            NWPlaceable bay = (_.CreateObject(OBJECT_TYPE_PLACEABLE, "fuel_bay", location));
            bay.AssignCommand(() => _.SetFacingPoint(oPC.Position));

            tower.SetLocalObject("CONTROL_TOWER_FUEL_BAY", bay.Object);
            bay.SetLocalObject("CONTROL_TOWER_PARENT", tower.Object);
            bay.SetLocalInt("PC_BASE_STRUCTURE_ID", structureID);

            if (isStronidium)
            {
                if(structure.PCBase.ReinforcedFuel > 0)
                    _.CreateItemOnObject("stronidium", bay.Object, structure.PCBase.ReinforcedFuel);

                bay.SetLocalInt("CONTROL_TOWER_FUEL_TYPE", 1);
            }
            else
            {
                if (structure.PCBase.Fuel > 0)
                    _.CreateItemOnObject("fuel_cell", bay.Object, structure.PCBase.Fuel);
            }

            oPC.AssignCommand(() => _.ActionInteractObject(bay.Object));
        }

        private void OpenResourceBay()
        {
            NWPlaceable tower = (NWPlaceable)GetDialogTarget();
            NWPlayer oPC = GetPC();

            if (((NWPlaceable)tower.GetLocalObject("CONTROL_TOWER_RESOURCE_BAY")).IsValid)
            {
                oPC.FloatingText("Someone else is already accessing that structure's inventory. Please wait.");
                return;
            }

            int structureID = tower.GetLocalInt("PC_BASE_STRUCTURE_ID");
            var structure = _db.PCBaseStructures.Single(x => x.PCBaseStructureID == structureID);
            Location location = oPC.Location;
            NWPlaceable bay = (_.CreateObject(OBJECT_TYPE_PLACEABLE, "resource_bay", location));

            tower.SetLocalObject("CONTROL_TOWER_RESOURCE_BAY", bay.Object);
            bay.SetLocalObject("CONTROL_TOWER_PARENT", tower.Object);
            bay.SetLocalInt("PC_BASE_STRUCTURE_ID", structureID);

            foreach (var item in structure.PCBaseStructureItems)
            {
                _serialization.DeserializeItem(item.ItemObject, bay);
            }

            oPC.AssignCommand(() => _.ActionInteractObject(bay.Object));
        }

    }
}
