using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Legacy.Conversation
{
    public class ControlTower: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");

            var mainPage = new DialogPage(
                "",
                "Access Fuel Bay",
                "Access Stronidium Bay",
                "Access Resource Bay");

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }
        
        public override void Initialize()
        {
            var structureID = new Guid(GetDialogTarget().GetLocalString("PC_BASE_STRUCTURE_ID"));
            var structure = DataService.PCBaseStructure.GetByID(structureID);
            var pcBaseID = structure.PCBaseID;
            var pcBase = DataService.PCBase.GetByID(pcBaseID);

            var currentCPU = BaseService.GetCPUInUse(pcBaseID);
            var currentPower = BaseService.GetPowerInUse(pcBaseID);
            var maxCPU = BaseService.GetMaxBaseCPU(pcBaseID);
            var maxPower = BaseService.GetMaxBasePower(pcBaseID);

            var currentReinforcedFuel = pcBase.ReinforcedFuel;
            var currentFuel = pcBase.Fuel;
            var currentResources = DataService.PCBaseStructureItem.GetNumberOfItemsContainedBy(structure.ID);
            var maxReinforcedFuel = BaseService.CalculateMaxReinforcedFuel(pcBaseID);
            var maxFuel = BaseService.CalculateMaxFuel(pcBaseID);
            var maxResources = BaseService.CalculateResourceCapacity(pcBaseID);

            string time;
            if (pcBase.DateFuelEnds > DateTime.UtcNow)
            {
                var deltaTime = pcBase.DateFuelEnds - DateTime.UtcNow;

                var tower = BaseService.GetBaseControlTower(pcBaseID);

                if (tower == null)
                {
                    Console.WriteLine("Could not locate control tower in ControlTower -> Initialize. PCBaseID = " + pcBaseID);
                    return;
                }

                var towerStructure = DataService.BaseStructure.GetByID(tower.BaseStructureID);
                var fuelRating = towerStructure.FuelRating;
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
                
                var timeSpan = TimeSpan.FromMinutes(minutes * currentFuel) + deltaTime;
                time = Time.GetTimeLongIntervals(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, false);

                time = "Fuel will expire in " + time;
            }
            else
            {
                time = ColorToken.Red("Fuel has expired.");
            }

             

            var header = ColorToken.Green("Power: ") + currentPower + " / " + maxPower + "\n";
            header += ColorToken.Green("CPU: ") + currentCPU + " / " + maxCPU + "\n";
            header += ColorToken.Green("Fuel: ") + currentFuel + " / " + maxFuel + "\n";
            header += ColorToken.Green("Reinforced Fuel: ") + currentReinforcedFuel + " / " + maxReinforcedFuel + "\n";
            header += ColorToken.Green("Resource Bay: ") + currentResources + " / " + maxResources + "\n";
            header += time + "\n";
            header += "What would you like to do with this control tower?";

            SetPageHeader("MainPage", header);

            if (!BasePermissionService.HasBasePermission(GetPC(), structure.PCBaseID, BasePermission.CanManageBaseFuel))
            {
                SetResponseVisible("MainPage", 1, false);
                SetResponseVisible("MainPage", 2, false);
            }

            if (!BasePermissionService.HasBasePermission(GetPC(), structure.PCBaseID, BasePermission.CanAccessStructureInventory))
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
            var tower = (NWPlaceable)GetDialogTarget();
            var oPC = GetPC();

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
            var structure = DataService.PCBaseStructure.GetByID(structureID);
            var pcBase = DataService.PCBase.GetByID(structure.PCBaseID);
            var location = oPC.Location;
            bay = NWScript.CreateObject(ObjectType.Placeable, "fuel_bay", location);
            bay.AssignCommand(() => NWScript.SetFacingPoint(oPC.Position));

            tower.SetLocalObject("CONTROL_TOWER_FUEL_BAY", bay.Object);
            bay.SetLocalObject("CONTROL_TOWER_PARENT", tower.Object);
            bay.SetLocalString("PC_BASE_STRUCTURE_ID", structureID.ToString());

            if (isStronidium)
            {
                if(pcBase.ReinforcedFuel > 0)
                    NWScript.CreateItemOnObject("stronidium", bay.Object, pcBase.ReinforcedFuel);

                bay.SetLocalInt("CONTROL_TOWER_FUEL_TYPE", 1);
            }
            else
            {
                if (pcBase.Fuel > 0)
                    NWScript.CreateItemOnObject("fuel_cell", bay.Object, pcBase.Fuel);
            }

            oPC.AssignCommand(() => NWScript.ActionInteractObject(bay.Object));
        }

        private void OpenResourceBay()
        {
            var tower = (NWPlaceable)GetDialogTarget();
            var oPC = GetPC();


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
            
            var structureID = new Guid(tower.GetLocalString("PC_BASE_STRUCTURE_ID"));
            var structureItems = DataService.PCBaseStructureItem.GetAllByPCBaseStructureID(structureID);
            var location = oPC.Location;
            bay = NWScript.CreateObject(ObjectType.Placeable, "resource_bay", location);

            tower.SetLocalObject("CONTROL_TOWER_RESOURCE_BAY", bay.Object);
            bay.SetLocalObject("CONTROL_TOWER_PARENT", tower.Object);
            bay.SetLocalString("PC_BASE_STRUCTURE_ID", structureID.ToString());

            foreach (var item in structureItems)
            {
                SerializationService.DeserializeItem(item.ItemObject, bay);
            }

            oPC.AssignCommand(() => NWScript.ActionInteractObject(bay.Object));
        }

    }
}
