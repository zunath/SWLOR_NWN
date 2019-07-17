using System;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Dialog;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service;
using BaseStructureType = SWLOR.Game.Server.Enumeration.BaseStructureType;

namespace SWLOR.Game.Server.Conversation
{
    public class BaseManagementTool : ConversationBase
    {
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
            DialogPage renamePage = new DialogPage("Type a name into the chat box. Once you are done select next.",
                "Next");
            DialogPage confirmRenamePage = new DialogPage(
                "<SET LATER>",
                "Confirm Name Change"
            );
            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("PurchaseTerritoryPage", purchaseTerritoryPage);
            dialog.AddPage("StructureListPage", structureListPage);
            dialog.AddPage("ManageStructureDetailsPage", manageStructureDetailsPage);
            dialog.AddPage("RetrieveStructurePage", retrievePage);
            dialog.AddPage("RotatePage", rotatePage);
            dialog.AddPage("RenamePage", renamePage);
            dialog.AddPage("ConfirmRenamePage", confirmRenamePage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadMainPage();
        }

        private void LoadMainPage()
        {
            ClearPageResponses("MainPage");
            var data = BaseService.GetPlayerTempData(GetPC());
            int cellX = (int)(_.GetPositionFromLocation(data.TargetLocation).m_X / 10.0f);
            int cellY = (int)(_.GetPositionFromLocation(data.TargetLocation).m_Y / 10.0f);
            string sector = BaseService.GetSectorOfLocation(data.TargetLocation);

            Area dbArea = DataService.Area.GetByResref(data.TargetArea.Resref);
            bool hasUnclaimed = false;
            Guid playerID = GetPC().GlobalID;
            int buildingTypeID = data.TargetArea.GetLocalInt("BUILDING_TYPE");
            Enumeration.BuildingType buildingType = buildingTypeID <= 0 ? Enumeration.BuildingType.Exterior : (Enumeration.BuildingType)buildingTypeID;
            data.BuildingType = buildingType;
            bool canEditBasePermissions = false;
            bool canEditBuildingPermissions = false;
            bool canEditBuildingPublicPermissions = false;
            bool canEditStructures = false;
            bool canEditPrimaryResidence = false;
            bool canRemovePrimaryResidence = false;
            bool canRenameStructure = false;
            bool canChangeStructureMode = false;
            bool canEditPublicBasePermissions = false;

            string header = ColorTokenService.Green("Base Management Menu\n\n");
            header += ColorTokenService.Green("Area: ") + data.TargetArea.Name + " (" + cellX + ", " + cellY + ")\n\n";

            // Are we in a starship?
            if (buildingType == Enumeration.BuildingType.Starship)
            {
                Guid pcBaseStructureID = new Guid(data.TargetArea.GetLocalString("PC_BASE_STRUCTURE_ID"));
                var structure = DataService.PCBaseStructure.GetByID(pcBaseStructureID);
                var buildingStyle = DataService.BuildingStyle.GetByID(Convert.ToInt32(structure.InteriorStyleID));
                int itemLimit = buildingStyle.FurnitureLimit + structure.StructureBonus;
                var childStructures = DataService.PCBaseStructure.GetAllByParentPCBaseStructureID(structure.ID);
                header += ColorTokenService.Green("Structure Limit: ") + childStructures.Count() + " / " + itemLimit + "\n";
                // Get all child structures contained by this building which improve atmosphere.
                var structures = DataService.PCBaseStructure.GetAllByParentPCBaseStructureID(pcBaseStructureID).Where(x =>
                {
                    var childStructure = DataService.BaseStructure.GetByID(x.BaseStructureID);
                    return childStructure.HasAtmosphere;
                });

                // Add up the total atmosphere rating, being careful not to go over the cap.
                int bonus = structures.Sum(x => 1 + x.StructureBonus) * 2;
                if (bonus > 150) bonus = 150;
                header += ColorTokenService.Green("Atmosphere Bonus: ") + bonus + "% / " + "150%";
                header += "\n";

                canEditPrimaryResidence = BasePermissionService.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanEditPrimaryResidence);
                canRemovePrimaryResidence = BasePermissionService.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanRemovePrimaryResidence);
                canRenameStructure = BasePermissionService.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanRenameStructures);
                canEditStructures = BasePermissionService.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanPlaceEditStructures);
                canEditBuildingPermissions = BasePermissionService.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanAdjustPermissions);
                canEditBuildingPublicPermissions = BasePermissionService.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanAdjustPublicPermissions);
                canChangeStructureMode = false; // Starships cannot be workshops.
                data.StructureID = pcBaseStructureID;
            }
            // Area is not buildable.
            else if (!dbArea.IsBuildable)
            {
                header += "Land in this area cannot be claimed. However, you can still manage any leases you own from the list below.";
            }
            // Building type is an interior of a building
            else if (buildingType == Enumeration.BuildingType.Interior)
            {
                Guid pcBaseStructureID = new Guid(data.TargetArea.GetLocalString("PC_BASE_STRUCTURE_ID"));
                var structure = DataService.PCBaseStructure.GetByID(pcBaseStructureID);
                var baseStructure = DataService.BaseStructure.GetByID(structure.BaseStructureID);
                int itemLimit = baseStructure.Storage + structure.StructureBonus;
                var childStructures = DataService.PCBaseStructure.GetAllByParentPCBaseStructureID(structure.ID);
                header += ColorTokenService.Green("Structure Limit: ") + childStructures.Count() + " / " + itemLimit + "\n";
                // Get all child structures contained by this building which improve atmosphere.
                var structures = DataService.PCBaseStructure.GetAllByParentPCBaseStructureID(pcBaseStructureID).Where(x =>
                {
                    var childStructure = DataService.BaseStructure.GetByID(x.BaseStructureID);
                    return childStructure.HasAtmosphere;
                });

                // Add up the total atmosphere rating, being careful not to go over the cap.
                int bonus = structures.Sum(x => 1 + x.StructureBonus) * 2;
                if (bonus > 150) bonus = 150;
                header += ColorTokenService.Green("Atmosphere Bonus: ") + bonus + "% / " + "150%";
                header += "\n";
                // The building must be set to the "Residence" mode in order for a primary resident to be selected.
                if (structure.StructureModeID == (int)StructureModeType.Residence)
                {
                    canEditPrimaryResidence = BasePermissionService.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanEditPrimaryResidence);
                    canRemovePrimaryResidence = BasePermissionService.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanRemovePrimaryResidence);
                }
                canRenameStructure = BasePermissionService.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanRenameStructures);
                canEditStructures = BasePermissionService.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanPlaceEditStructures);
                canEditBuildingPermissions = BasePermissionService.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanAdjustPermissions);
                canEditBuildingPublicPermissions = BasePermissionService.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanAdjustPublicPermissions);
                canChangeStructureMode = BasePermissionService.HasStructurePermission(GetPC(), pcBaseStructureID, StructurePermission.CanChangeStructureMode);
                data.StructureID = pcBaseStructureID;
            }
            // Building type is an apartment
            // Apartments may only ever be in the "Residence" mode.
            else if (buildingType == Enumeration.BuildingType.Apartment)
            {
                Guid pcBaseID = new Guid(data.TargetArea.GetLocalString("PC_BASE_ID"));
                var pcBase = DataService.PCBase.GetByID(pcBaseID);
                var buildingStyle = DataService.BuildingStyle.GetByID(Convert.ToInt32(pcBase.BuildingStyleID));
                int itemLimit = buildingStyle.FurnitureLimit;
                var structures = DataService.PCBaseStructure.GetAllByPCBaseID(pcBase.ID);
                header += ColorTokenService.Green("Structure Limit: ") + structures.Count() + " / " + itemLimit + "\n";
                // Add up the total atmosphere rating, being careful not to go over the cap.
                int bonus = structures.Sum(x => 1 + x.StructureBonus) * 2;
                if (bonus > 150) bonus = 150;
                header += ColorTokenService.Green("Atmosphere Bonus: ") + bonus + "% / " + "150%";
                header += "\n";
                canEditStructures = BasePermissionService.HasBasePermission(GetPC(), pcBaseID, BasePermission.CanPlaceEditStructures);
                canEditBasePermissions = BasePermissionService.HasBasePermission(GetPC(), pcBaseID, BasePermission.CanAdjustPermissions);
                canEditPrimaryResidence = BasePermissionService.HasBasePermission(GetPC(), pcBaseID, BasePermission.CanEditPrimaryResidence);
                canRemovePrimaryResidence = BasePermissionService.HasBasePermission(GetPC(), pcBaseID, BasePermission.CanRemovePrimaryResidence);
                canRenameStructure = BasePermissionService.HasBasePermission(GetPC(), pcBaseID, BasePermission.CanRenameStructures);
                data.PCBaseID = pcBaseID;
            }
            // Building type is an exterior building
            else if (buildingType == Enumeration.BuildingType.Exterior)
            {

                var pcBase = DataService.PCBase.GetByAreaResrefAndSectorOrDefault(data.TargetArea.Resref, sector);

                var northeastOwner = dbArea.NortheastOwner == null ? null : DataService.Player.GetByID((Guid)dbArea.NortheastOwner);
                var northwestOwner = dbArea.NorthwestOwner == null ? null : DataService.Player.GetByID((Guid)dbArea.NorthwestOwner);
                var southeastOwner = dbArea.SoutheastOwner == null ? null : DataService.Player.GetByID((Guid)dbArea.SoutheastOwner);
                var southwestOwner = dbArea.SouthwestOwner == null ? null : DataService.Player.GetByID((Guid)dbArea.SouthwestOwner);

                if (northeastOwner != null)
                {
                    header += ColorTokenService.Green("Northeast Owner: ") + "Claimed";
                    if (dbArea.NortheastOwner == playerID)
                        header += " (" + northeastOwner.CharacterName + ")";
                    header += "\n";
                }
                else
                {
                    header += ColorTokenService.Green("Northeast Owner: ") + "Unclaimed\n";
                    hasUnclaimed = true;
                }

                if (northwestOwner != null)
                {
                    header += ColorTokenService.Green("Northwest Owner: ") + "Claimed";
                    if (dbArea.NorthwestOwner == playerID)
                        header += " (" + northwestOwner.CharacterName + ")";
                    header += "\n";
                }
                else
                {
                    header += ColorTokenService.Green("Northwest Owner: ") + "Unclaimed\n";
                    hasUnclaimed = true;
                }

                if (southeastOwner != null)
                {
                    header += ColorTokenService.Green("Southeast Owner: ") + "Claimed";
                    if (dbArea.SoutheastOwner == playerID)
                        header += " (" + southeastOwner.CharacterName + ")";
                    header += "\n";
                }
                else
                {
                    header += ColorTokenService.Green("Southeast Owner: ") + "Unclaimed\n";
                    hasUnclaimed = true;
                }

                if (southwestOwner != null)
                {
                    header += ColorTokenService.Green("Southwest Owner: ") + "Claimed";
                    if (dbArea.SouthwestOwner == playerID)
                        header += " (" + southwestOwner.CharacterName + ")";
                    header += "\n";
                }
                else
                {
                    header += ColorTokenService.Green("Southwest Owner: ") + "Unclaimed\n";
                    hasUnclaimed = true;
                }

                canEditStructures = pcBase != null && BasePermissionService.HasBasePermission(GetPC(), pcBase.ID, BasePermission.CanPlaceEditStructures);
                canEditBasePermissions = pcBase != null && BasePermissionService.HasBasePermission(GetPC(), pcBase.ID, BasePermission.CanAdjustPermissions);
                canEditPublicBasePermissions = pcBase != null && BasePermissionService.HasBasePermission(GetPC(), pcBase.ID, BasePermission.CanAdjustPublicPermissions);
                if (pcBase != null)
                    data.PCBaseID = pcBase.ID;
            }
            else
            {
                throw new Exception("BaseManagementTool -> Cannot locate building type with ID " + buildingTypeID);
            }

            SetPageHeader("MainPage", header);

            bool showManage = DataService.PCBasePermission.GetAllByPlayerID(GetPC().GlobalID).Count(x => x.CanExtendLease) > 0;
            AddResponseToPage("MainPage", "Manage My Leases", showManage);
            AddResponseToPage("MainPage", "Purchase Territory", hasUnclaimed && dbArea.IsBuildable);
            AddResponseToPage("MainPage", "Edit Nearby Structures", canEditStructures);
            AddResponseToPage("MainPage", "Edit Base Permissions", canEditBasePermissions || canEditPublicBasePermissions);
            AddResponseToPage("MainPage", "Edit Building Permissions", canEditBuildingPermissions || canEditBuildingPublicPermissions);
            AddResponseToPage("MainPage", "Edit Primary Residence", canEditPrimaryResidence || canRemovePrimaryResidence);
            AddResponseToPage("MainPage", "Rename Building", canRenameStructure);
            AddResponseToPage("MainPage", "Edit Building Mode", canChangeStructureMode);
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
                case "RenamePage":
                    GetPC().SetLocalInt("LISTENING_FOR_DESCRIPTION", 1);
                    RenameResponses(responseID);
                    break;
                case "ConfirmRenamePage":
                    HandleConfirmSetNameResponse(responseID);
                    break;
            }
        }
        private void RenameResponses(int responseID)
        {
            switch(responseID)
            {
                case 1:
                    string newDescription = GetPC().GetLocalString("NEW_DESCRIPTION_TO_SET");

                    if (string.IsNullOrWhiteSpace(newDescription))
                    {
                        _.FloatingTextStringOnCreature("Type in a new name to the chat bar and then press 'Next'.", GetPC().Object, _.FALSE);
                        return;
                    }

                    string header = "Your new name follows. If you need to make a change, click 'Back', type in a new description, and then hit 'Next' again.\n\n";
                    header += ColorTokenService.Green("New Description: ") + "\n\n";
                    header += newDescription;
                    SetPageHeader("ConfirmRenamePage", header);
                    ChangePage("ConfirmRenamePage");
                    break;
                case 2:
                    GetPC().DeleteLocalInt("LISTENING_FOR_DESCRIPTION");
                    GetPC().DeleteLocalString("NEW_DESCRIPTION_TO_SET");
                    break;
            }
        }
        private void HandleConfirmSetNameResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Confirm Description Change
                    var data = BaseService.GetPlayerTempData(GetPC());
                    int buildingTypeID = data.TargetArea.GetLocalInt("BUILDING_TYPE");
                    Enumeration.BuildingType buildingType = buildingTypeID <= 0 ? Enumeration.BuildingType.Exterior : (Enumeration.BuildingType)buildingTypeID;
                    data.BuildingType = buildingType;
                    NWPlayer sender = _.GetPCSpeaker();

                    if (buildingType == Enumeration.BuildingType.Apartment)
                    {
                        // Update the base name. 
                        Guid pcBaseID = new Guid(data.TargetArea.GetLocalString("PC_BASE_ID"));
                        var pcBase = DataService.PCBase.GetByID(pcBaseID);
                        pcBase.CustomName = GetPC().GetLocalString("NEW_DESCRIPTION_TO_SET");
                        DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);
                        sender.SendMessage("Name is now set to " + pcBase.CustomName);
                    }
                    else if (buildingType == Enumeration.BuildingType.Interior)
                    {
                        // Update the structure name.
                        Guid pcBaseStructureID = new Guid(data.TargetArea.GetLocalString("PC_BASE_STRUCTURE_ID"));
                        var structure = DataService.PCBaseStructure.GetByID(pcBaseStructureID);
                        structure.CustomName = GetPC().GetLocalString("NEW_DESCRIPTION_TO_SET");
                        DataService.SubmitDataChange(structure, DatabaseActionType.Update);
                        sender.SendMessage("Name is now set to " + structure.CustomName);
                    }
                    else if (buildingType == Enumeration.BuildingType.Starship)
                    {
                        // Note - starships need to record the name in both the base and the structure entries.
                        Guid pcBaseStructureID = new Guid(data.TargetArea.GetLocalString("PC_BASE_STRUCTURE_ID"));
                        var structure = DataService.PCBaseStructure.GetByID(pcBaseStructureID);
                        structure.CustomName = GetPC().GetLocalString("NEW_DESCRIPTION_TO_SET");
                        DataService.SubmitDataChange(structure, DatabaseActionType.Update);

                        var pcBase = DataService.PCBase.GetByID(structure.PCBaseID);
                        pcBase.CustomName = GetPC().GetLocalString("NEW_DESCRIPTION_TO_SET");
                        DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);

                        sender.SendMessage("Name is now set to " + structure.CustomName);
                    }

                    EndConversation();
                    break;
            }
        }
        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
            var data = BaseService.GetPlayerTempData(GetPC());
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

            switch (afterMovePage)
            {
                case "MainPage":
                    GetPC().DeleteLocalInt("LISTENING_FOR_DESCRIPTION");
                    GetPC().DeleteLocalString("NEW_DESCRIPTION_TO_SET");
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
                case 7: // Rename Building/Apartment
                    GetPC().SetLocalInt("LISTENING_FOR_DESCRIPTION", 1);
                    _.FloatingTextStringOnCreature("Type in a new name to the chat bar and then press 'Next'.", GetPC().Object, _.FALSE);
                    ChangePage("RenamePage");
                    break;
                case 8: // Edit Building Mode
                    SwitchConversation("EditBuildingMode");
                    break;
            }
        }

        private string BuildPurchaseTerritoryHeader()
        {
            var data = BaseService.GetPlayerTempData(GetPC());
            Area dbArea = DataService.Area.GetByResref(data.TargetArea.Resref);
            var player = DataService.Player.GetByID(GetPC().GlobalID);
            int purchasePrice = dbArea.PurchasePrice + (int)(dbArea.PurchasePrice * (player.LeaseRate * 0.01f));
            int dailyUpkeep = dbArea.DailyUpkeep + (int) (dbArea.DailyUpkeep * (player.LeaseRate * 0.01f));

            string header = ColorTokenService.Green("Purchase Territory Menu\n\n");
            header += "Land leases in this sector cost an initial price of " + purchasePrice + " credits.\n\n";
            header += "You will also be billed " + dailyUpkeep + " credits per day (real world time). Your initial payment covers the cost of the first week.\n\n";
            header += "Purchasing territory gives you the ability to place a control tower, drill for raw materials, construct buildings, build starships, and much more.\n\n";
            header += "You will have a chance to review your purchase before confirming.";

            return header;
        }

        private void LoadPurchaseTerritoryResponses()
        {
            ClearPageResponses("PurchaseTerritoryPage");
            var data = BaseService.GetPlayerTempData(GetPC());
            Area dbArea = DataService.Area.GetByResref(data.TargetArea.Resref);

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
            var data = BaseService.GetPlayerTempData(GetPC());

            if (data.IsConfirming && data.ConfirmingPurchaseResponseID == responseID)
            {
                BaseService.PurchaseArea(GetPC(), data.TargetArea, sector);
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
            var data = BaseService.GetPlayerTempData(GetPC());

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
            var data = BaseService.GetPlayerTempData(GetPC());
            string pcBaseStructureID = data.TargetArea.GetLocalString("PC_BASE_STRUCTURE_ID");
            bool isBuilding = !string.IsNullOrWhiteSpace(pcBaseStructureID);

            IEnumerable<AreaStructure> areaStructures = data.TargetArea.Data["BASE_SERVICE_STRUCTURES"];
            if (!isBuilding)
            {
                string targetSector = BaseService.GetSectorOfLocation(data.TargetLocation);

                areaStructures = areaStructures
                    .Where(x => BaseService.GetSectorOfLocation(x.Structure.Location) == targetSector &&
                                x.IsEditable &&
                                _.GetDistanceBetweenLocations(x.Structure.Location, data.TargetLocation) <= 15.0f);
            }

            areaStructures = areaStructures.OrderBy(o => _.GetDistanceBetweenLocations(o.Structure.Location, data.TargetLocation));

            foreach (var structure in areaStructures)
            {
                AddResponseToPage("StructureListPage", structure.Structure.Name, true, structure);
            }
        }

        private void StructureListResponses(int responseID)
        {
            DialogResponse response = GetResponseByID("StructureListPage", responseID);
            AreaStructure structure = (AreaStructure)response.CustomData;
            var data = BaseService.GetPlayerTempData(GetPC());
            data.ManipulatingStructure = structure;
            
            LoadManageStructureDetails();
            ChangePage("ManageStructureDetailsPage");
        }

        private void LoadManageStructureDetails()
        {
            ClearPageResponses("ManageStructureDetailsPage");
            var data = BaseService.GetPlayerTempData(GetPC());
            var structure = data.ManipulatingStructure.Structure;
            var pcBaseStructureID = data.ManipulatingStructure.Structure.Area.GetLocalString("PC_BASE_STRUCTURE_ID");
            Guid? structureID = string.IsNullOrWhiteSpace(pcBaseStructureID) ? null : (Guid?)new Guid(pcBaseStructureID);
            bool canRetrieveStructures;
            bool canPlaceEditStructures;
            if (structureID != null)
            {
                canRetrieveStructures = BasePermissionService.HasStructurePermission(GetPC(), (Guid)structureID, StructurePermission.CanRetrieveStructures);
                canPlaceEditStructures = BasePermissionService.HasStructurePermission(GetPC(), (Guid)structureID, StructurePermission.CanPlaceEditStructures);
            }
            else
            {
                canRetrieveStructures = BasePermissionService.HasBasePermission(GetPC(), data.ManipulatingStructure.PCBaseID, BasePermission.CanRetrieveStructures);
                canPlaceEditStructures = BasePermissionService.HasBasePermission(GetPC(), data.ManipulatingStructure.PCBaseID, BasePermission.CanPlaceEditStructures);
            }


            string header = ColorTokenService.Green("Structure: ") + structure.Name + "\n\n";
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
            var data = BaseService.GetPlayerTempData(GetPC());
            PCBaseStructure structure = DataService.PCBaseStructure.GetByID(data.ManipulatingStructure.PCBaseStructureID);
            BaseStructure baseStructure = DataService.BaseStructure.GetByID(structure.BaseStructureID);
            PCBase pcBase = DataService.PCBase.GetByID(structure.PCBaseID);
            BaseStructureType structureType = (BaseStructureType)baseStructure.BaseStructureTypeID;
            var tempStorage = _.GetObjectByTag("TEMP_ITEM_STORAGE");
            var pcStructureID = structure.ID;
            int impoundedCount = 0;

            var controlTower = BaseService.GetBaseControlTower(pcBase.ID);
            int maxShields = BaseService.CalculateMaxShieldHP(controlTower);

            if (structureType == BaseStructureType.Starship)
            {
                GetPC().SendMessage("You cannot pick up starships once they are built.  You can only fly them away.");
                return;
            }

            if (pcBase.PCBaseTypeID != (int) Enumeration.PCBaseType.Starship && pcBase.ShieldHP < maxShields && structureType != BaseStructureType.ControlTower)
            {
                GetPC().FloatingText("You cannot retrieve any structures because the control tower has less than 100% shields.");
                return;
            }

            bool canRetrieveStructures;

            if (data.BuildingType == Enumeration.BuildingType.Exterior ||
                data.BuildingType == Enumeration.BuildingType.Apartment)
            {
                canRetrieveStructures = BasePermissionService.HasBasePermission(GetPC(), data.ManipulatingStructure.PCBaseID, BasePermission.CanRetrieveStructures);
            }
            else if (data.BuildingType == Enumeration.BuildingType.Interior || data.BuildingType == Enumeration.BuildingType.Starship)
            {
                var structureID = new Guid(data.ManipulatingStructure.Structure.Area.GetLocalString("PC_BASE_STRUCTURE_ID"));
                canRetrieveStructures = BasePermissionService.HasStructurePermission(GetPC(), structureID, StructurePermission.CanRetrieveStructures);
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
                var structureCount = DataService.PCBaseStructure.GetAllByPCBaseID(structure.PCBaseID).Count();

                if (structureCount > 1)
                {
                    GetPC().FloatingText("You must remove all structures in this sector before picking up the control tower.");
                    return;
                }

                // Impound resources retrieved by drills.
                var items = DataService.PCBaseStructureItem.GetAllByPCBaseStructureID(structure.ID);
                foreach(var item in items)
                {
                    ImpoundService.Impound(item);
                    DataService.SubmitDataChange(item, DatabaseActionType.Delete);
                    impoundedCount++;
                }
            }
            else if (structureType == BaseStructureType.Building)
            {
                var childStructures = DataService.PCBaseStructure.GetAllByParentPCBaseStructureID(structure.ID).ToList();
                for (int x = childStructures.Count - 1; x >= 0; x--)
                {
                    var furniture = childStructures.ElementAt(x);
                    NWItem furnitureItem = BaseService.ConvertStructureToItem(furniture, tempStorage);
                    ImpoundService.Impound(GetPC().GlobalID, furnitureItem);
                    furnitureItem.Destroy();

                    DataService.SubmitDataChange(furniture, DatabaseActionType.Delete);
                    impoundedCount++;
                }
                
                // Remove any primary owner permissions.
                var primaryOwner = DataService.Player.GetByPrimaryResidencePCBaseStructureIDOrDefault(structure.ID);
                if (primaryOwner != null)
                {
                    primaryOwner.PrimaryResidencePCBaseStructureID = null;
                    DataService.SubmitDataChange(primaryOwner, DatabaseActionType.Update);
                }

                // Remove any access permissions.
                foreach (var buildingPermission in DataService.PCBaseStructurePermission.GetAllByPCBaseStructureID(structure.ID))
                {
                    DataService.SubmitDataChange(buildingPermission, DatabaseActionType.Delete);
                }

            }
            else if (structureType == BaseStructureType.StarshipProduction && data.ManipulatingStructure.Structure.GetLocalInt("DOCKED_STARSHIP") == 1)
            {
                GetPC().SendMessage("You cannot move a dock that has a starship docked in it.  Fly the ship away first.");
                return;
            }

            BaseService.ConvertStructureToItem(structure, GetPC());
            DataService.SubmitDataChange(structure, DatabaseActionType.Delete);
            data.ManipulatingStructure.Structure.Destroy();

            // Impound any fuel that's over the limit.
            if (structureType == BaseStructureType.StronidiumSilo || structureType == BaseStructureType.FuelSilo)
            {
                int maxFuel = BaseService.CalculateMaxFuel(pcBase.ID);
                int maxReinforcedFuel = BaseService.CalculateMaxReinforcedFuel(pcBase.ID);

                if (pcBase.Fuel > maxFuel)
                {
                    int returnAmount = pcBase.Fuel - maxFuel;
                    NWItem refund = _.CreateItemOnObject("fuel_cell", tempStorage, returnAmount);
                    pcBase.Fuel = maxFuel;
                    ImpoundService.Impound(pcBase.PlayerID, refund);
                    GetPC().SendMessage("Excess fuel cells have been impounded by the planetary government. The owner of the base will need to retrieve it.");
                    refund.Destroy();
                }

                if (pcBase.ReinforcedFuel > maxReinforcedFuel)
                {
                    int returnAmount = pcBase.ReinforcedFuel - maxReinforcedFuel;
                    NWItem refund = _.CreateItemOnObject("stronidium", tempStorage, returnAmount);
                    pcBase.ReinforcedFuel = maxReinforcedFuel;
                    ImpoundService.Impound(pcBase.PlayerID, refund);
                    GetPC().SendMessage("Excess stronidium units have been impounded by the planetary government. The owner of the base will need to retrieve it.");
                    refund.Destroy();
                }
            }
            else if (structureType == BaseStructureType.ResourceSilo)
            {
                int maxResources = BaseService.CalculateResourceCapacity(pcBase.ID);
                var items = DataService.PCBaseStructureItem.GetAllByPCBaseStructureID(controlTower.ID).ToList();

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

                    DataService.SubmitDataChange(impoundItem, DatabaseActionType.Insert);
                    GetPC().SendMessage(item.ItemName + " has been impounded by the planetary government because your base ran out of space to store resources. The owner of the base will need to retrieve it.");
                    DataService.SubmitDataChange(item, DatabaseActionType.Delete);
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
            var data = BaseService.GetPlayerTempData(GetPC());
            var structure = data.ManipulatingStructure.Structure;
            float facing = structure.Facing;
            string header = ColorTokenService.Green("Current Direction: ") + facing;

            SetPageHeader("RotatePage", header);
        }

        private void DoRotate(float degrees, bool isSet)
        {
            var data = BaseService.GetPlayerTempData(GetPC());
            bool canPlaceEditStructures;
            var structure = data.ManipulatingStructure.Structure;

            if (data.BuildingType == Enumeration.BuildingType.Exterior ||
                data.BuildingType == Enumeration.BuildingType.Apartment)
            {
                canPlaceEditStructures = BasePermissionService.HasBasePermission(GetPC(), data.ManipulatingStructure.PCBaseID, BasePermission.CanPlaceEditStructures);
            }
            else if (data.BuildingType == Enumeration.BuildingType.Interior)
            {
                var structureID = new Guid(data.ManipulatingStructure.Structure.Area.GetLocalString("PC_BASE_STRUCTURE_ID"));
                canPlaceEditStructures = BasePermissionService.HasStructurePermission(GetPC(), structureID, StructurePermission.CanPlaceEditStructures);
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

            var dbStructure = DataService.PCBaseStructure.GetByID(data.ManipulatingStructure.PCBaseStructureID);
            var baseStructure = DataService.BaseStructure.GetByID(dbStructure.BaseStructureID);
            dbStructure.LocationOrientation = facing;

            if (baseStructure.BaseStructureTypeID == (int)BaseStructureType.Building)
            {
                // The structure's facing isn't updated until after this code executes.
                // Build a new location object for use with spawning the door.
                var exteriorStyle = DataService.BuildingStyle.GetByID(Convert.ToInt32(dbStructure.ExteriorStyleID));

                Location locationOverride = _.Location(data.TargetArea.Object,
                    structure.Position,
                    facing);
                data.ManipulatingStructure.ChildStructure.Destroy();
                data.ManipulatingStructure.ChildStructure = BaseService.SpawnBuildingDoor(exteriorStyle.DoorRule, structure, locationOverride);

                // Update the cache
                List<AreaStructure> areaStructures = data.TargetArea.Data["BASE_SERVICE_STRUCTURES"];
                var cacheStructure = areaStructures.Single(x => x.PCBaseStructureID == data.ManipulatingStructure.PCBaseStructureID && x.ChildStructure == null);
                int doorIndex = areaStructures.IndexOf(cacheStructure);
                areaStructures[doorIndex].Structure = data.ManipulatingStructure.ChildStructure;
            }

            DataService.SubmitDataChange(dbStructure, DatabaseActionType.Update);
        }

        public override void EndDialog()
        {
            GetPC().DeleteLocalInt("LISTENING_FOR_DESCRIPTION");
            GetPC().DeleteLocalString("NEW_DESCRIPTION_TO_SET");
            BaseService.ClearPlayerTempData(GetPC());     
        }
    }
}
