using System;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Dialog;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using BaseStructureType = SWLOR.Game.Server.Enumeration.BaseStructureType;
using BuildingType = SWLOR.Game.Server.Data.Entity.BuildingType;

namespace SWLOR.Game.Server.Conversation
{
    public class BaseManagementTool : ConversationBase
    {
        private readonly IBaseService _base;
        private readonly IColorTokenService _color;
        private readonly IDataService _data;
        private readonly IImpoundService _impound;
        private readonly IBasePermissionService _perm;

        public BaseManagementTool(
            INWScript script,
            IDialogService dialog,
            IBaseService @base,
            IColorTokenService color,
            IDataService data,
            IImpoundService impound,
            IBasePermissionService perm)
            : base(script, dialog)
        {
            _base = @base;
            _color = color;
            _data = data;
            _impound = impound;
            _perm = perm;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage();
            DialogPage purchaseTerritoryPage = new DialogPage();
            DialogPage structureListPage = new DialogPage("Select a structure to edit it. List is ordered by nearest structure to the location you selected. A maximum of 30 structures will be displayed at a time.");
            DialogPage manageStructureDetailsPage = new DialogPage();
            DialogPage retrievePage = new DialogPage("If this structure contains anything inside - such as items or furniture - they will be sent to the planetary government's impound. You will need to pay a fee to retrieve the items.\n\nAre you sure you want to retrieve this structure?",
                "Confirm Retrieve Structure");
            DialogPage rotatePage = new DialogPage(string.Empty,
                "East",
                "North",
                "West",
                "South",
                "20 degrees",
                "30 degrees",
                "45 degrees",
                "60 degrees",
                "75 degrees",
                "90 degrees",
                "180 degrees");
            

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("PurchaseTerritoryPage", purchaseTerritoryPage);
            dialog.AddPage("StructureListPage", structureListPage);
            dialog.AddPage("ManageStructureDetailsPage", manageStructureDetailsPage);
            dialog.AddPage("RetrieveStructurePage", retrievePage);
            dialog.AddPage("RotatePage", rotatePage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadMainPage();
        }

        private void LoadMainPage()
        {
            ClearPageResponses("MainPage");
            var data = _base.GetPlayerTempData(GetPC());
            int cellX = (int)(_.GetPositionFromLocation(data.TargetLocation).m_X / 10.0f);
            int cellY = (int)(_.GetPositionFromLocation(data.TargetLocation).m_Y / 10.0f);
            string sector = _base.GetSectorOfLocation(data.TargetLocation);

            Area dbArea = _data.Single<Area>(x => x.Resref == data.TargetArea.Resref);
            bool hasUnclaimed = false;
            Guid playerID = GetPC().GlobalID;
            Guid pcBaseID = new Guid(data.TargetArea.GetLocalString("PC_BASE_ID"));
            Guid pcBaseStructureID = new Guid(data.TargetArea.GetLocalString("PC_BASE_STRUCTURE_ID"));
            int buildingTypeID = data.TargetArea.GetLocalInt("BUILDING_TYPE");
            Enumeration.BuildingType buildingType = buildingTypeID <= 0 ? Enumeration.BuildingType.Exterior : (Enumeration.BuildingType)buildingTypeID;
            data.BuildingType = buildingType;

            bool canEditBasePermissions = false;
            bool canEditBuildingPermissions = false;
            bool canEditStructures;
            bool canEditPrimaryResidence = false;
            bool canRemovePrimaryResidence = false;

            if (buildingType == Enumeration.BuildingType.Interior)
            {
                canEditStructures = _perm.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanPlaceEditStructures);
                canEditBuildingPermissions = _perm.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanAdjustPermissions);
                canEditPrimaryResidence = _perm.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanEditPrimaryResidence);
                canRemovePrimaryResidence = _perm.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanRemovePrimaryResidence);
                data.StructureID = pcBaseStructureID;
            }
            else if (buildingType == Enumeration.BuildingType.Apartment)
            {
                canEditStructures = _perm.HasBasePermission(GetPC(), pcBaseID, BasePermission.CanPlaceEditStructures);
                canEditBasePermissions = _perm.HasBasePermission(GetPC(), pcBaseID, BasePermission.CanAdjustPermissions);
                canEditPrimaryResidence = _perm.HasBasePermission(GetPC(), pcBaseID, BasePermission.CanEditPrimaryResidence);
                canRemovePrimaryResidence = _perm.HasBasePermission(GetPC(), pcBaseID, BasePermission.CanRemovePrimaryResidence);
                data.PCBaseID = pcBaseID;
            }
            else if(buildingType == Enumeration.BuildingType.Exterior)
            {
                var pcBase = _data.SingleOrDefault<PCBase>(x => x.AreaResref == data.TargetArea.Resref && x.Sector == sector);
                canEditStructures = pcBase != null && _perm.HasBasePermission(GetPC(), pcBase.ID, BasePermission.CanPlaceEditStructures);
                canEditBasePermissions = pcBase != null && _perm.HasBasePermission(GetPC(), pcBase.ID, BasePermission.CanAdjustPermissions);
                if (pcBase != null)
                    data.PCBaseID = pcBase.ID;
            }
            else
            {
                throw new Exception("BaseManagementTool -> Cannot locate building type with ID " + buildingTypeID);
            }

            string header = _color.Green("Base Management Menu\n\n");
            header += _color.Green("Area: ") + data.TargetArea.Name + " (" + cellX + ", " + cellY + ")\n\n";

            if (!dbArea.IsBuildable)
            {
                header += "Land in this area cannot be claimed. However, you can still manage any leases you own from the list below.";
            }
            else if(buildingType == Enumeration.BuildingType.Interior)
            {
                var structure = _data.Single<PCBaseStructure>(x => x.ID == pcBaseStructureID);
                var baseStructure = _data.Get<BaseStructure>(structure.BaseStructureID);
                int itemLimit = baseStructure.Storage + structure.StructureBonus;
                var childStructures = _data.Where<PCBaseStructure>(x => x.ParentPCBaseStructureID == structure.ID);
                header += _color.Green("Item Limit: ") + childStructures.Count() + " / " + itemLimit + "\n";
            }
            else if (buildingType == Enumeration.BuildingType.Apartment)
            {
                var pcBase = _data.Get<PCBase>(pcBaseID);
                var buildingStyle = _data.Get<BuildingStyle>(pcBase.BuildingStyleID);
                int itemLimit = buildingStyle.FurnitureLimit;
                var structures = _data.Where<PCBaseStructure>(x => x.PCBaseID == pcBase.ID);
                header += _color.Green("Item Limit: ") + structures.Count() + " / " + itemLimit + "\n";
            }
            else if(buildingType == Enumeration.BuildingType.Exterior)
            {
                var northeastOwner = _data.Get<Player>(dbArea.NortheastOwner);
                var northwestOwner = _data.Get<Player>(dbArea.NorthwestOwner);
                var southeastOwner = _data.Get<Player>(dbArea.SoutheastOwner);
                var southwestOwner = _data.Get<Player>(dbArea.SouthwestOwner);

                if (northeastOwner != null)
                {
                    header += _color.Green("Northeast Owner: ") + "Claimed";
                    if (dbArea.NortheastOwner == playerID)
                        header += " (" + northeastOwner.CharacterName + ")";
                    header += "\n";
                }
                else
                {
                    header += _color.Green("Northeast Owner: ") + "Unclaimed\n";
                    hasUnclaimed = true;
                }

                if (northwestOwner != null)
                {
                    header += _color.Green("Northwest Owner: ") + "Claimed";
                    if (dbArea.NorthwestOwner == playerID)
                        header += " (" + northwestOwner.CharacterName + ")";
                    header += "\n";
                }
                else
                {
                    header += _color.Green("Northwest Owner: ") + "Unclaimed\n";
                    hasUnclaimed = true;
                }

                if (southeastOwner != null)
                {
                    header += _color.Green("Southeast Owner: ") + "Claimed";
                    if (dbArea.SoutheastOwner == playerID)
                        header += " (" + southeastOwner.CharacterName + ")";
                    header += "\n";
                }
                else
                {
                    header += _color.Green("Southeast Owner: ") + "Unclaimed\n";
                    hasUnclaimed = true;
                }

                if (southwestOwner != null)
                {
                    header += _color.Green("Southwest Owner: ") + "Claimed";
                    if (dbArea.SouthwestOwner == playerID)
                        header += " (" + southwestOwner.CharacterName + ")";
                    header += "\n";
                }
                else
                {
                    header += _color.Green("Southwest Owner: ") + "Unclaimed\n";
                    hasUnclaimed = true;
                }
            }

            SetPageHeader("MainPage", header);

            bool showManage = _data.GetAll<PCBase>().Count(x => x.PlayerID == playerID) > 0;
            

            AddResponseToPage("MainPage", "Manage My Leases", showManage);
            AddResponseToPage("MainPage", "Purchase Territory", hasUnclaimed && dbArea.IsBuildable);
            AddResponseToPage("MainPage", "Edit Nearby Structures", canEditStructures);
            AddResponseToPage("MainPage", "Edit Base Permissions", canEditBasePermissions);
            AddResponseToPage("MainPage", "Edit Building Permissions", canEditBuildingPermissions);
            AddResponseToPage("MainPage", "Edit Primary Residence", canEditPrimaryResidence || canRemovePrimaryResidence);
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainResponses(responseID);
                    break;
                case "PurchaseTerritoryPage":
                    PurchaseTerritoryResponses(responseID);
                    break;
                case "StructureListPage":
                    StructureListResponses(responseID);
                    break;
                case "ManageStructureDetailsPage":
                    ManageStructureResponses(responseID);
                    break;
                case "RetrieveStructurePage":
                    RetrieveStructureResponses(responseID);
                    break;
                case "RotatePage":
                    RotateResponses(responseID);
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
            var data = _base.GetPlayerTempData(GetPC());
            switch (beforeMovePage)
            {
                case "PurchaseTerritoryPage":
                    data.IsConfirming = false;
                    data.ConfirmingPurchaseResponseID = 0;
                    LoadMainPage();
                    break;
                case "StructureListPage":
                    data.ManipulatingStructure = null;
                    break;
            }
        }

        private void MainResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // Manage my lease
                    SwitchConversation("ManageLease");
                    break;
                case 2: // Purchase territory
                    SetPageHeader("PurchaseTerritoryPage", BuildPurchaseTerritoryHeader());
                    LoadPurchaseTerritoryResponses();
                    ChangePage("PurchaseTerritoryPage");
                    break;
                case 3: // Manage nearby structures
                    LoadManageStructuresPage();
                    ChangePage("StructureListPage");
                    break;
                case 4: // Edit base permissions
                    SwitchConversation("EditBasePermissions");
                    break;
                case 5: // Edit building permissions
                    SwitchConversation("EditBuildingPermissions");
                    break;
                case 6: // Edit primary residence
                    SwitchConversation("EditPrimaryResidence");
                    break;
            }
        }

        private string BuildPurchaseTerritoryHeader()
        {
            var data = _base.GetPlayerTempData(GetPC());
            Area dbArea = _data.Single<Area>(x => x.Resref == data.TargetArea.Resref);
            string header = _color.Green("Purchase Territory Menu\n\n");
            header += "Land leases in this sector cost an initial price of " + dbArea.PurchasePrice + " credits.\n\n";
            header += "You will also be billed " + dbArea.DailyUpkeep + " credits per day (real world time). Your initial payment covers the cost of the first week.\n\n";
            header += "Purchasing territory gives you the ability to place a control tower, drill for raw materials, construct buildings, build starships, and much more.\n\n";
            header += "You will have a chance to review your purchase before confirming.";

            return header;
        }

        private void LoadPurchaseTerritoryResponses()
        {
            ClearPageResponses("PurchaseTerritoryPage");
            var data = _base.GetPlayerTempData(GetPC());
            Area dbArea = _data.Single<Area>(x => x.Resref == data.TargetArea.Resref);

            AddResponseToPage("PurchaseTerritoryPage", "Purchase Northeast Sector", dbArea.NortheastOwner == null);
            AddResponseToPage("PurchaseTerritoryPage", "Purchase Northwest Sector", dbArea.NorthwestOwner == null);
            AddResponseToPage("PurchaseTerritoryPage", "Purchase Southeast Sector", dbArea.SoutheastOwner == null);
            AddResponseToPage("PurchaseTerritoryPage", "Purchase Southwest Sector", dbArea.SouthwestOwner == null);
        }

        private void PurchaseTerritoryResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // Northeast sector
                    DoBuy(AreaSector.Northeast, responseID);
                    break;
                case 2: // Northwest sector
                    DoBuy(AreaSector.Northwest, responseID);
                    break;
                case 3: // Southeast sector
                    DoBuy(AreaSector.Southeast, responseID);
                    break;
                case 4: // Southwest sector
                    DoBuy(AreaSector.Southwest, responseID);
                    break;
            }
        }

        private void DoBuy(string sector, int responseID)
        {
            var data = _base.GetPlayerTempData(GetPC());

            if (data.IsConfirming && data.ConfirmingPurchaseResponseID == responseID)
            {
                _base.PurchaseArea(GetPC(), data.TargetArea, sector);
                data.IsConfirming = false;
                RefreshPurchaseResponses();
                LoadMainPage();
                ClearNavigationStack();
                ChangePage("MainPage", false);
            }
            else if (data.IsConfirming && data.ConfirmingPurchaseResponseID != responseID)
            {
                data.ConfirmingPurchaseResponseID = responseID;
                RefreshPurchaseResponses();
            }
            else
            {
                data.IsConfirming = true;
                data.ConfirmingPurchaseResponseID = responseID;
                RefreshPurchaseResponses();
            }

        }

        private void RefreshPurchaseResponses()
        {
            var data = _base.GetPlayerTempData(GetPC());

            SetResponseText("PurchaseTerritoryPage", 1,
                data.ConfirmingPurchaseResponseID == 1 ?
                    "CONFIRM PURCHASE NORTHEAST SECTOR" :
                    "Purchase Northeast Sector");
            SetResponseText("PurchaseTerritoryPage", 2,
                data.ConfirmingPurchaseResponseID == 2 ?
                    "CONFIRM PURCHASE NORTHWEST SECTOR" :
                    "Purchase Northwest Sector");
            SetResponseText("PurchaseTerritoryPage", 3,
                data.ConfirmingPurchaseResponseID == 3 ?
                    "CONFIRM PURCHASE SOUTHEAST SECTOR" :
                    "Purchase Southeast Sector");
            SetResponseText("PurchaseTerritoryPage", 4,
                data.ConfirmingPurchaseResponseID == 4 ?
                    "CONFIRM PURCHASE SOUTHWEST SECTOR" :
                    "Purchase Southwest Sector");
        }

        private void LoadManageStructuresPage()
        {
            ClearPageResponses("StructureListPage");
            var data = _base.GetPlayerTempData(GetPC());
            int pcBaseStructureID = data.TargetArea.GetLocalInt("PC_BASE_STRUCTURE_ID");
            bool isBuilding = pcBaseStructureID > 0;

            List<AreaStructure> areaStructures = data.TargetArea.Data["BASE_SERVICE_STRUCTURES"]; ;
            if (!isBuilding)
            {
                string targetSector = _base.GetSectorOfLocation(data.TargetLocation);

                areaStructures = areaStructures
                    .Where(x => _base.GetSectorOfLocation(x.Structure.Location) == targetSector &&
                                x.IsEditable &&
                                _.GetDistanceBetweenLocations(x.Structure.Location, data.TargetLocation) <= 15.0f)
                    .OrderBy(o => _.GetDistanceBetweenLocations(o.Structure.Location, data.TargetLocation))
                    .ToList();

            }

            foreach (var structure in areaStructures)
            {
                AddResponseToPage("StructureListPage", structure.Structure.Name, true, structure);
            }
        }

        private void StructureListResponses(int responseID)
        {
            DialogResponse response = GetResponseByID("StructureListPage", responseID);
            AreaStructure structure = (AreaStructure)response.CustomData;
            var data = _base.GetPlayerTempData(GetPC());
            data.ManipulatingStructure = structure;
            
            LoadManageStructureDetails();
            ChangePage("ManageStructureDetailsPage");
        }

        private void LoadManageStructureDetails()
        {
            ClearPageResponses("ManageStructureDetailsPage");
            var data = _base.GetPlayerTempData(GetPC());
            var structure = data.ManipulatingStructure.Structure;
            Guid structureID = new Guid(data.ManipulatingStructure.Structure.Area.GetLocalString("PC_BASE_STRUCTURE_ID"));
            bool canRetrieveStructures;
            bool canPlaceEditStructures;
            if (structureID != Guid.Empty)
            {
                canRetrieveStructures = _perm.HasStructurePermission(GetPC(), structureID, StructurePermission.CanRetrieveStructures);
                canPlaceEditStructures = _perm.HasStructurePermission(GetPC(), structureID, StructurePermission.CanPlaceEditStructures);
            }
            else
            {
                canRetrieveStructures = _perm.HasBasePermission(GetPC(), data.ManipulatingStructure.PCBaseID, BasePermission.CanRetrieveStructures);
                canPlaceEditStructures = _perm.HasBasePermission(GetPC(), data.ManipulatingStructure.PCBaseID, BasePermission.CanPlaceEditStructures);
            }


            string header = _color.Green("Structure: ") + structure.Name + "\n\n";
            header += "What would you like to do with this structure?";

            SetPageHeader("ManageStructureDetailsPage", header);

            AddResponseToPage("ManageStructureDetailsPage", "Retrieve Structure", canRetrieveStructures);
            AddResponseToPage("ManageStructureDetailsPage", "Rotate", canPlaceEditStructures);
        }

        private void ManageStructureResponses(int responseID)
        {
            switch (responseID)
            {
                case 1:
                    ChangePage("RetrieveStructurePage");
                    break;
                case 2:
                    LoadRotatePage();
                    ChangePage("RotatePage");
                    break;
                case 3:
                    LoadManageStructuresPage();
                    ChangePage("StructureListPage");
                    break;
            }
        }

        private void RetrieveStructureResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // Confirm retrieve structure
                    DoRetrieveStructure();
                    break;
            }
        }

        private void DoRetrieveStructure()
        {
            var data = _base.GetPlayerTempData(GetPC());
            PCBaseStructure structure = _data.Get<PCBaseStructure>(data.ManipulatingStructure.PCBaseStructureID);
            BaseStructure baseStructure = _data.Get<BaseStructure>(structure.BaseStructureID);
            PCBase pcBase = _data.Get<PCBase>(structure.PCBaseID);
            BaseStructureType structureType = (BaseStructureType)baseStructure.BaseStructureTypeID;
            var tempStorage = _.GetObjectByTag("TEMP_ITEM_STORAGE");
            var pcStructureID = structure.ID;
            int impoundedCount = 0;
            var structureID = new Guid(data.ManipulatingStructure.Structure.Area.GetLocalString("PC_BASE_STRUCTURE_ID"));

            var controlTower = _base.GetBaseControlTower(pcBase.ID);
            int maxShields = _base.CalculateMaxShieldHP(controlTower);

            if (pcBase.ShieldHP < maxShields && structureType != BaseStructureType.ControlTower)
            {
                GetPC().FloatingText("You cannot retrieve any structures because the control tower has less than 100% shields.");
                return;
            }

            bool canRetrieveStructures;

            if (data.BuildingType == Enumeration.BuildingType.Exterior ||
                data.BuildingType == Enumeration.BuildingType.Apartment)
            {
                canRetrieveStructures = _perm.HasBasePermission(GetPC(), data.ManipulatingStructure.PCBaseID, BasePermission.CanRetrieveStructures);
            }
            else if (data.BuildingType == Enumeration.BuildingType.Interior)
            {
                canRetrieveStructures = _perm.HasStructurePermission(GetPC(), structureID, StructurePermission.CanRetrieveStructures);
            }
            else
            {
                throw new Exception("BaseManagementTool -> DoRetrieveStructure: Cannot handle building type " + data.BuildingType);
            }

            if (!canRetrieveStructures)
            {
                GetPC().FloatingText("You don't have permission to retrieve structures.");
                return;
            }


            if (structureType == BaseStructureType.ControlTower)
            {
                var structureCount = _data.GetAll<PCBaseStructure>().Count(x => x.PCBaseID == structure.PCBaseID);

                if (structureCount > 1)
                {
                    GetPC().FloatingText("You must remove all structures in this sector before picking up the control tower.");
                    return;
                }

            }
            else if (structureType == BaseStructureType.Building)
            {
                var childStructures = _data.Where<PCBaseStructure>(x => x.ParentPCBaseStructureID == structure.ID).ToList();
                for (int x = childStructures.Count - 1; x >= 0; x--)
                {
                    var furniture = childStructures.ElementAt(x);
                    NWItem furnitureItem = _base.ConvertStructureToItem(furniture, tempStorage);
                    _impound.Impound(GetPC().GlobalID, furnitureItem);
                    furnitureItem.Destroy();

                    _data.SubmitDataChange(furniture, DatabaseActionType.Delete);
                    impoundedCount++;
                }
            }

            _base.ConvertStructureToItem(structure, GetPC());
            _data.SubmitDataChange(structure, DatabaseActionType.Delete);
            data.ManipulatingStructure.Structure.Destroy();

            // Impound any fuel that's over the limit.
            if (structureType == BaseStructureType.StronidiumSilo || structureType == BaseStructureType.FuelSilo)
            {
                int maxFuel = _base.CalculateMaxFuel(pcBase);
                int maxReinforcedFuel = _base.CalculateMaxReinforcedFuel(pcBase);

                if (pcBase.Fuel > maxFuel)
                {
                    int returnAmount = pcBase.Fuel - maxFuel;
                    NWItem refund = _.CreateItemOnObject("fuel_cell", tempStorage, returnAmount);
                    pcBase.Fuel = maxFuel;
                    _impound.Impound(pcBase.PlayerID, refund);
                    GetPC().SendMessage("Excess fuel cells have been impounded by the planetary government. The owner of the base will need to retrieve it.");
                    refund.Destroy();
                }

                if (pcBase.ReinforcedFuel > maxReinforcedFuel)
                {
                    int returnAmount = pcBase.ReinforcedFuel - maxReinforcedFuel;
                    NWItem refund = _.CreateItemOnObject("stronidium", tempStorage, returnAmount);
                    pcBase.ReinforcedFuel = maxReinforcedFuel;
                    _impound.Impound(pcBase.PlayerID, refund);
                    GetPC().SendMessage("Excess stronidium units have been impounded by the planetary government. The owner of the base will need to retrieve it.");
                    refund.Destroy();
                }
            }
            else if (structureType == BaseStructureType.ResourceSilo)
            {
                int maxResources = _base.CalculateResourceCapacity(pcBase);
                var items = _data.Where<PCBaseStructureItem>(x => x.PCBaseStructureID == controlTower.ID).ToList();

                while (items.Count > maxResources)
                {
                    var item = items.ElementAt(0);

                    var impoundItem = new PCImpoundedItem
                    {
                        PlayerID = pcBase.PlayerID,
                        ItemResref = item.ItemResref,
                        ItemObject = item.ItemObject,
                        DateImpounded = DateTime.UtcNow,
                        ItemName = item.ItemName,
                        ItemTag = item.ItemTag
                    };

                    _data.SubmitDataChange(impoundItem, DatabaseActionType.Insert);
                    GetPC().SendMessage(item.ItemName + " has been impounded by the planetary government because your base ran out of space to store resources. The owner of the base will need to retrieve it.");
                    _data.SubmitDataChange(item, DatabaseActionType.Delete);
                }
            }

            // Update the cache
            List<AreaStructure> areaStructures = data.TargetArea.Data["BASE_SERVICE_STRUCTURES"];
            var records = areaStructures.Where(x => x.PCBaseStructureID == pcStructureID).ToList();
            for (int x = records.Count() - 1; x >= 0; x--)
            {
                var record = records[x];
                record.ChildStructure?.Destroy();
                areaStructures.Remove(record);
            }

            EndConversation();

            if(impoundedCount > 0)
                GetPC().FloatingText(impoundedCount + " item(s) were sent to the planetary impound.");
        }

        private void RotateResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // East
                    DoRotate(0.0f, true);
                    break;
                case 2: // North
                    DoRotate(90.0f, true);
                    break;
                case 3: // West
                    DoRotate(180.0f, true);
                    break;
                case 4: // South
                    DoRotate(270.0f, true);
                    break;
                case 5: // Rotate 20
                    DoRotate(20.0f, false);
                    break;
                case 6: // Rotate 30
                    DoRotate(30.0f, false);
                    break;
                case 7: // Rotate 45
                    DoRotate(45.0f, false);
                    break;
                case 8: // Rotate 60
                    DoRotate(60.0f, false);
                    break;
                case 9: // Rotate 75
                    DoRotate(75.0f, false);
                    break;
                case 10: // Rotate 90
                    DoRotate(90.0f, false);
                    break;
                case 11: // Rotate 180
                    DoRotate(180.0f, false);
                    break;
            }
        }

        private void LoadRotatePage()
        {
            var data = _base.GetPlayerTempData(GetPC());
            var structure = data.ManipulatingStructure.Structure;
            float facing = structure.Facing;
            string header = _color.Green("Current Direction: ") + facing;

            SetPageHeader("RotatePage", header);
        }

        private void DoRotate(float degrees, bool isSet)
        {
            var data = _base.GetPlayerTempData(GetPC());
            bool canPlaceEditStructures;
            var structure = data.ManipulatingStructure.Structure;

            if (data.BuildingType == Enumeration.BuildingType.Exterior ||
                data.BuildingType == Enumeration.BuildingType.Apartment)
            {
                canPlaceEditStructures = _perm.HasBasePermission(GetPC(), data.ManipulatingStructure.PCBaseID, BasePermission.CanPlaceEditStructures);
            }
            else if (data.BuildingType == Enumeration.BuildingType.Interior)
            {
                var structureID = new Guid(data.ManipulatingStructure.Structure.Area.GetLocalString("PC_BASE_STRUCTURE_ID"));
                canPlaceEditStructures = _perm.HasStructurePermission(GetPC(), structureID, StructurePermission.CanPlaceEditStructures);
            }
            else
            {
                throw new Exception("BaseManagementTool -> DoRotate: Cannot handle building type " + data.BuildingType);
            }

            if (!canPlaceEditStructures)
            {
                GetPC().FloatingText("You don't have permission to edit structures.");
                return;
            }


            float facing = structure.Facing;
            if (isSet)
            {
                facing = degrees;
            }
            else
            {
                facing += degrees;
            }

            while (facing > 360)
            {
                facing -= 360;
            }

            structure.Facing = facing;
            LoadRotatePage();

            var dbStructure = _data.Single<PCBaseStructure>(x => x.ID == data.ManipulatingStructure.PCBaseStructureID);
            var baseStructure = _data.Get<BaseStructure>(dbStructure.BaseStructureID);
            dbStructure.LocationOrientation = facing;

            if (baseStructure.BaseStructureTypeID == (int)BaseStructureType.Building)
            {
                // The structure's facing isn't updated until after this code executes.
                // Build a new location object for use with spawning the door.
                var exteriorStyle = _data.Get<BuildingStyle>(dbStructure.ExteriorStyleID);

                Location locationOverride = _.Location(data.TargetArea.Object,
                    structure.Position,
                    facing);
                data.ManipulatingStructure.ChildStructure.Destroy();
                data.ManipulatingStructure.ChildStructure = _base.SpawnBuildingDoor(exteriorStyle.DoorRule, structure, locationOverride);

                // Update the cache
                List<AreaStructure> areaStructures = data.TargetArea.Data["BASE_SERVICE_STRUCTURES"];
                var cacheStructure = areaStructures.Single(x => x.PCBaseStructureID == data.ManipulatingStructure.PCBaseStructureID && x.ChildStructure == null);
                int doorIndex = areaStructures.IndexOf(cacheStructure);
                areaStructures[doorIndex].Structure = data.ManipulatingStructure.ChildStructure;
            }

            _data.SubmitDataChange(dbStructure, DatabaseActionType.Update);
        }

        public override void EndDialog()
        {
            _base.ClearPlayerTempData(GetPC());
        }
    }
}
