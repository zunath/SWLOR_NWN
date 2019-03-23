using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN._;

namespace SWLOR.Game.Server.Conversation
{
    public class ControlTower: ConversationBase
    {
        private readonly IBaseService _base;
        
        private readonly IBasePermissionService _perm;
        private readonly ISerializationService _serialization;
        

        public ControlTower(
             
            IDialogService dialog,
            
            IBasePermissionService perm,
            ISerializationService serialization,
            IBaseService @base
            ) 
            : base(dialog)
        {
            
            _perm = perm;
            _serialization = serialization;
            _base = @base;
            
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "",
                "Access Fuel Bay",
                "Access Stronidium Bay",
                "Access Resource Bay");

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }
        
        public override void Initialize()
        {
            Guid structureID = new Guid(GetDialogTarget().GetLocalString("PC_BASE_STRUCTURE_ID"));
            PCBaseStructure structure = DataService.Single<PCBaseStructure>(x => x.ID == structureID);
            Guid pcBaseID = structure.PCBaseID;
            PCBase pcBase = DataService.Get<PCBase>(pcBaseID);

            double currentCPU = _base.GetCPUInUse(pcBaseID);
            double currentPower = _base.GetPowerInUse(pcBaseID);
            double maxCPU = _base.GetMaxBaseCPU(pcBaseID);
            double maxPower = _base.GetMaxBasePower(pcBaseID);

            int currentReinforcedFuel = pcBase.ReinforcedFuel;
            int currentFuel = pcBase.Fuel;
            int currentResources = DataService.Where<PCBaseStructureItem>(x => x.PCBaseStructureID == structure.ID).Count();
            int maxReinforcedFuel = _base.CalculateMaxReinforcedFuel(pcBaseID);
            int maxFuel = _base.CalculateMaxFuel(pcBaseID);
            int maxResources = _base.CalculateResourceCapacity(pcBaseID);

            string time;
            if (pcBase.DateFuelEnds > DateTime.UtcNow)
            {
                TimeSpan deltaTime = pcBase.DateFuelEnds - DateTime.UtcNow;

                var tower = _base.GetBaseControlTower(pcBaseID);
                var towerStructure = DataService.Single<BaseStructure>(x => x.ID == tower.BaseStructureID);
                int fuelRating = towerStructure.FuelRating;
                int minutes;

                switch (fuelRating)
                {
                    case 1: // Small
                        minutes = 45;
                        break;
                    case 2: // Medium
                        minutes = 15;
                        break;
                    case 3: // Large
                        minutes = 5;
                        break;
                    default:
                        throw new Exception("Invalid fuel rating value: " + fuelRating);
                }
                
                TimeSpan timeSpan = TimeSpan.FromMinutes(minutes * currentFuel) + deltaTime;
                time = TimeService.GetTimeLongIntervals(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, false);

                time = "Fuel will expire in " + time;
            }
            else
            {
                time = ColorTokenService.Red("Fuel has expired.");
            }

             

            string header = ColorTokenService.Green("Power: ") + currentPower + " / " + maxPower + "\n";
            header += ColorTokenService.Green("CPU: ") + currentCPU + " / " + maxCPU + "\n";
            header += ColorTokenService.Green("Fuel: ") + currentFuel + " / " + maxFuel + "\n";
            header += ColorTokenService.Green("Reinforced Fuel: ") + currentReinforcedFuel + " / " + maxReinforcedFuel + "\n";
            header += ColorTokenService.Green("Resource Bay: ") + currentResources + " / " + maxResources + "\n";
            header += time + "\n";
            header += "What would you like to do with this control tower?";

            SetPageHeader("MainPage", header);

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
            var structure = DataService.Single<PCBaseStructure>(x => x.ID == structureID);
            var pcBase = DataService.Get<PCBase>(structure.PCBaseID);
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
            var structureItems = DataService.Where<PCBaseStructureItem>(x => x.PCBaseStructureID == structureID);
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
