using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Conversation
{
    public class ControlTower: ConversationBase
    {
        private readonly IDataService _data;
        private readonly IBasePermissionService _perm;
        private readonly ISerializationService _serialization;

        public ControlTower(
            INWScript script, 
            IDialogService dialog,
            IDataService data,
            IBasePermissionService perm,
            ISerializationService serialization) 
            : base(script, dialog)
        {
            _data = data;
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
            Guid structureID = new Guid(GetDialogTarget().GetLocalString("PC_BASE_STRUCTURE_ID"));
            PCBaseStructure structure = _data.Single<PCBaseStructure>(x => x.ID == structureID);

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

            NWPlaceable bay = tower.GetLocalObject("CONTROL_TOWER_FUEL_BAY");
            if (bay.IsValid)
            {
                NWObject accessor = bay.GetLocalObject("BAY_ACCESSOR");
                if (!accessor.IsValid)
                {
                    bay.Destroy();
                }
                else
                {
                    oPC.FloatingText("Someone else is already accessing that structure's inventory. Please wait.");
                    return;
                }
            }

            var structureID = new Guid(tower.GetLocalString("PC_BASE_STRUCTURE_ID"));
            var structure = _data.Single<PCBaseStructure>(x => x.ID == structureID);
            var pcBase = _data.Get<PCBase>(structure.PCBaseID);
            Location location = oPC.Location;
            bay = _.CreateObject(OBJECT_TYPE_PLACEABLE, "fuel_bay", location);
            bay.AssignCommand(() => _.SetFacingPoint(oPC.Position));

            tower.SetLocalObject("CONTROL_TOWER_FUEL_BAY", bay.Object);
            bay.SetLocalObject("CONTROL_TOWER_PARENT", tower.Object);
            bay.SetLocalString("PC_BASE_STRUCTURE_ID", structureID.ToString());

            if (isStronidium)
            {
                if(pcBase.ReinforcedFuel > 0)
                    _.CreateItemOnObject("stronidium", bay.Object, pcBase.ReinforcedFuel);

                bay.SetLocalInt("CONTROL_TOWER_FUEL_TYPE", 1);
            }
            else
            {
                if (pcBase.Fuel > 0)
                    _.CreateItemOnObject("fuel_cell", bay.Object, pcBase.Fuel);
            }

            oPC.AssignCommand(() => _.ActionInteractObject(bay.Object));
        }

        private void OpenResourceBay()
        {
            NWPlaceable tower = (NWPlaceable)GetDialogTarget();
            NWPlayer oPC = GetPC();


            NWPlaceable bay = tower.GetLocalObject("CONTROL_TOWER_RESOURCE_BAY");
            if (bay.IsValid)
            {
                NWObject accessor = bay.GetLocalObject("BAY_ACCESSOR");
                if (!accessor.IsValid)
                {
                    bay.Destroy();
                }
                else
                {
                    oPC.FloatingText("Someone else is already accessing that structure's inventory. Please wait.");
                    return;
                }
            }
            
            Guid structureID = new Guid(tower.GetLocalString("PC_BASE_STRUCTURE_ID"));
            var structureItems = _data.Where<PCBaseStructureItem>(x => x.PCBaseStructureID == structureID);
            Location location = oPC.Location;
            bay = _.CreateObject(OBJECT_TYPE_PLACEABLE, "resource_bay", location);

            tower.SetLocalObject("CONTROL_TOWER_RESOURCE_BAY", bay.Object);
            bay.SetLocalObject("CONTROL_TOWER_PARENT", tower.Object);
            bay.SetLocalString("PC_BASE_STRUCTURE_ID", structureID.ToString());

            foreach (var item in structureItems)
            {
                _serialization.DeserializeItem(item.ItemObject, bay);
            }

            oPC.AssignCommand(() => _.ActionInteractObject(bay.Object));
        }

    }
}
