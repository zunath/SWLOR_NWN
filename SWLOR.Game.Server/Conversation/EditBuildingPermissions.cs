using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.ValueObject.Dialog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Conversation
{
    public class EditBuildingPermissions: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");

            var mainPage = new DialogPage(
                "Settings adjusted here will only affect this particular structure. If this is a building, the permissions will affect everything inside the building.",
                "Change Player Permissions",
                "Change Public Permissions");

            var playerListPage = new DialogPage("Please select a player.");
            var playerDetailsPage = new DialogPage();
            var publicPermissionsPage = new DialogPage();

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("PlayerListPage", playerListPage);
            dialog.AddPage("PlayerDetailsPage", playerDetailsPage);
            dialog.AddPage("PublicPermissionsPage", publicPermissionsPage);
            return dialog;
        }

        public override void Initialize()
        {
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainResponses(responseID);
                    break;
                case "PlayerListPage":
                    PlayerListResponses(responseID);
                    break;
                case "PlayerDetailsPage":
                    PlayerDetailsResponses(responseID);
                    break;
                case "PublicPermissionsPage":
                    PublicPermissionsResponses(responseID);
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
            switch (beforeMovePage)
            {
                case "PlayerDetailsPage":
                    BuildPlayerListPage();
                    break;
            }
        }

        private void MainResponses(int responseID)
        {
            var data = BaseService.GetPlayerTempData(GetPC());

            switch (responseID)
            {
                case 1: // Change Player Permissions
                    if (!BasePermissionService.HasStructurePermission(GetPC(), data.StructureID, StructurePermission.CanAdjustPermissions))
                    {
                        GetPC().FloatingText("You do not have permission to change other players' permissions.");
                        return;
                    }

                    BuildPlayerListPage();
                    ChangePage("PlayerListPage");
                    break;
                case 2: // Change Public Permissions
                    if (!BasePermissionService.HasStructurePermission(GetPC(), data.StructureID, StructurePermission.CanAdjustPublicPermissions))
                    {
                        GetPC().FloatingText("You do not have permission to change this building's PUBLIC permissions.");
                        return;
                    }

                    BuildPublicPermissionsPage();
                    ChangePage("PublicPermissionsPage");
                    break; 
            }
        }

        private void BuildPlayerListPage()
        {
            var speakingPC = GetPC();
            var playerIdList = new List<Guid>();

            ClearPageResponses("PlayerListPage");

            // Online players
            foreach (var player in NWModule.Get().Players)
            {
                if (player == speakingPC || !player.IsPlayer) continue;
                playerIdList.Add(player.GlobalID);
                AddResponseToPage("PlayerListPage", player.Name + ColorTokenService.Green(" online"), true, DataService.Player.GetByID(player.GlobalID));
            }

            // Offline players with existing permissions
            var data = BaseService.GetPlayerTempData(GetPC());
            var permissions = DataService.PCBasePermission.GetAllPermissionsByPCBaseID(data.PCBaseID);
            foreach (var permission in permissions)
            {
                var player = DataService.Player.GetByID(permission.PlayerID);

                // don't allow deletion of the self!
                if (player.ID == speakingPC.GlobalID || playerIdList.Contains(player.ID)) continue;

                playerIdList.Add(player.ID);
                AddResponseToPage("PlayerListPage", player.CharacterName + ColorTokenService.Red(" offline"), true, player);
            }
        }

        private void PlayerListResponses(int responseID)
        {
            var response = GetResponseByID("PlayerListPage", responseID);
            var player = (Player)response.CustomData;

            BuildPlayerDetailsPage(player);
            ChangePage("PlayerDetailsPage");
        }

        private void BuildPlayerDetailsPage(Player player)
        {
            ClearPageResponses("PlayerDetailsPage");
            var data = BaseService.GetPlayerTempData(GetPC());
            var permission = DataService.PCBaseStructurePermission.GetPlayerPrivatePermissionOrDefault(player.ID, data.StructureID);

            // Intentionally excluded permissions: CanAdjustPermissions, CanCancelLease
            var canPlaceEditStructures = permission?.CanPlaceEditStructures ?? false;
            var canAccessStructureInventory = permission?.CanAccessStructureInventory ?? false;
            var canEnterBuilding = permission?.CanEnterBuilding ?? false;
            var canAdjustPermissions = permission?.CanAdjustPermissions ?? false;
            var canRetrieveStructures = permission?.CanRetrieveStructures ?? false;
            var canRenameStructures = permission?.CanRenameStructures ?? false;
            var canEditPrimaryResidence = permission?.CanEditPrimaryResidence ?? false;
            var canRemovePrimaryResidence = permission?.CanRemovePrimaryResidence ?? false;
            var canChangeStructureMode = permission?.CanChangeStructureMode ?? false;
            var canAdjustPublicPermissions = permission?.CanAdjustPublicPermissions ?? false;
            var canFlyStarship = permission?.CanFlyStarship ?? false;

            var area = GetArea(GetPC());
            var isStarship = GetLocalInt(area, "BUILDING_TYPE") == (int)Enumeration.BuildingType.Starship;

            var header = ColorTokenService.Green("Name: ") + player.CharacterName + "\n\n";

            header += ColorTokenService.Green("Permissions:\n\n");
            header += "Can Place/Edit Structures: " + (canPlaceEditStructures ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Access Structure Inventory: " + (canAccessStructureInventory ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Enter Building: " + (canEnterBuilding ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Adjust Permissions: " + (canAdjustPermissions ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Retrieve Structures: " + (canRetrieveStructures ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Rename Structures: " + (canRenameStructures ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Edit Primary Residence: " + (canEditPrimaryResidence ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Remove Primary Residence: " + (canRemovePrimaryResidence ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Change Structure Mode: " + (canChangeStructureMode ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Adjust PUBLIC Permissions: " + (canAdjustPublicPermissions ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";

            if (isStarship) header += "Can Fly Starship: " + (canFlyStarship ? ColorTokenService.Green("YES") :ColorTokenService.Red("NO")) + "\n";

            SetPageHeader("PlayerDetailsPage", header);

            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Place/Edit Structures", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Access Structure Inventory", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Enter Building", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Adjust Permissions", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Retrieve Structures", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Rename Structures", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Edit Primary Residence", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Remove Primary Residence", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Change Structure Mode", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Adjust PUBLIC Permissions", true, player);
            AddResponseToPage("PlayerDetailsPage", ColorTokenService.Red("WARNING") + ": Delete Player Permissions", true, player);
            // Add new non-conditional responses here to avoid confusing the response handling logic.
            if (isStarship) AddResponseToPage("PlayerDetailsPage", "Toggle: Can Fly Starship", true, player);            
        }

        private void PlayerDetailsResponses(int responseID)
        {
            var response = GetResponseByID("PlayerDetailsPage", responseID);
            var player = (Player)response.CustomData;

            switch (responseID)
            {
                case 1: // Can Place/Edit Structures
                    TogglePermission(player.ID, StructurePermission.CanPlaceEditStructures, false);
                    break;
                case 2: // Can Access Structure Inventory
                    TogglePermission(player.ID, StructurePermission.CanAccessStructureInventory, false);
                    break;
                case 3: // Can Enter Building
                    TogglePermission(player.ID, StructurePermission.CanEnterBuilding, false);
                    break;
                case 4: // Can Adjust Permissions
                    TogglePermission(player.ID, StructurePermission.CanAdjustPermissions, false);
                    break;
                case 5: // Can Retrieve Structures
                    TogglePermission(player.ID, StructurePermission.CanRetrieveStructures, false);
                    break;
                case 6: // Can Rename Structures
                    TogglePermission(player.ID, StructurePermission.CanRenameStructures, false);
                    break;
                case 7: // Can Edit Primary Residence
                    TogglePermission(player.ID, StructurePermission.CanEditPrimaryResidence, false);
                    break;
                case 8: // Can Remove Primary Residence
                    TogglePermission(player.ID, StructurePermission.CanRemovePrimaryResidence, false);
                    break;
                case 9: // Can Change Structure Mode
                    TogglePermission(player.ID, StructurePermission.CanChangeStructureMode, false);
                    break;
                case 10: // Can Adjust PUBLIC Permissions
                    TogglePermission(player.ID, StructurePermission.CanAdjustPublicPermissions, false);
                    break;
                case 11: // Delete this Players Permissions object 
                    DeletePlayerPermission(player.ID, BasePermission.CanAdjustPublicPermissions, false);
                    break;
                case 12: // Can fly starship
                    TogglePermission(player.ID, StructurePermission.CanFlyStarship, false);
                    break;
            }

            BuildPlayerDetailsPage(player);
        }

        private void TogglePermission(Guid playerID, StructurePermission permission, bool isPublicPermission)
        {
            var data = BaseService.GetPlayerTempData(GetPC());
            var dbPermission = isPublicPermission ?
                DataService.PCBaseStructurePermission.GetPublicPermissionOrDefault(data.StructureID) :
                DataService.PCBaseStructurePermission.GetPlayerPrivatePermissionOrDefault(playerID, data.StructureID);

            var action = DatabaseActionType.Update;

            if (dbPermission == null)
            {
                dbPermission = new PCBaseStructurePermission()
                {
                    PCBaseStructureID = data.StructureID,
                    PlayerID = playerID,
                    IsPublicPermission = isPublicPermission
                };
                action = DatabaseActionType.Insert;
            }

            switch (permission)
            {
                case StructurePermission.CanPlaceEditStructures:
                    dbPermission.CanPlaceEditStructures = !dbPermission.CanPlaceEditStructures;
                    break;
                case StructurePermission.CanAccessStructureInventory:
                    dbPermission.CanAccessStructureInventory = !dbPermission.CanAccessStructureInventory;
                    break;
                case StructurePermission.CanEnterBuilding:
                    dbPermission.CanEnterBuilding = !dbPermission.CanEnterBuilding;
                    break;
                case StructurePermission.CanAdjustPermissions:
                    dbPermission.CanAdjustPermissions = !dbPermission.CanAdjustPermissions;
                    break;
                case StructurePermission.CanRetrieveStructures:
                    dbPermission.CanRetrieveStructures = !dbPermission.CanRetrieveStructures;
                    break;
                case StructurePermission.CanRenameStructures:
                    dbPermission.CanRenameStructures = !dbPermission.CanRenameStructures;
                    break;
                case StructurePermission.CanEditPrimaryResidence:
                    dbPermission.CanEditPrimaryResidence = !dbPermission.CanEditPrimaryResidence;
                    break;
                case StructurePermission.CanRemovePrimaryResidence:
                    dbPermission.CanRemovePrimaryResidence = !dbPermission.CanRemovePrimaryResidence;
                    break;
                case StructurePermission.CanChangeStructureMode:
                    dbPermission.CanChangeStructureMode = !dbPermission.CanChangeStructureMode;
                    break;
                case StructurePermission.CanAdjustPublicPermissions:
                    dbPermission.CanAdjustPublicPermissions = !dbPermission.CanAdjustPublicPermissions;
                    break;
                case StructurePermission.CanFlyStarship:
                    dbPermission.CanFlyStarship = !dbPermission.CanFlyStarship;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(permission), permission, null);
            }

            DataService.SubmitDataChange(dbPermission, action);
        }
        private void DeletePlayerPermission(Guid playerID, BasePermission permission, bool isPublicPermission)
        {
            var data = BaseService.GetPlayerTempData(GetPC());
            var dbPermission = isPublicPermission ?
                DataService.PCBasePermission.GetPublicPermissionOrDefault(data.PCBaseID) :
                DataService.PCBasePermission.GetPlayerPrivatePermissionOrDefault(playerID, data.PCBaseID);

            DataService.SubmitDataChange(dbPermission, DatabaseActionType.Delete);
        }
        private void BuildPublicPermissionsPage()
        {
            ClearPageResponses("PublicPermissionsPage");
            var data = BaseService.GetPlayerTempData(GetPC());
            var permission = DataService.PCBaseStructurePermission.GetPublicPermissionOrDefault(data.StructureID);

            // Intentionally excluded permissions:
            // CanAdjustPermissions, CanCancelLease, CanPlaceEditStructures, CanAccessStructureInventory, CanAdjustPermissions,
            // CanRetrieveStructures, CanRenameStructures, CanEditPrimaryResidence, CanRemovePrimaryResidence, CanChangeStructureMode,
            // CanAdjustPublicPermissions
            var canEnterBuilding = permission?.CanEnterBuilding ?? false;

            var header = ColorTokenService.Green("Public Permissions: ") + "\n\n";
            header += "Can Enter Building: " + (canEnterBuilding ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";

            SetPageHeader("PublicPermissionsPage", header);

            AddResponseToPage("PublicPermissionsPage", "Toggle: Can Enter Building");
        }

        private void PublicPermissionsResponses(int responseID)
        {
            var data = BaseService.GetPlayerTempData(GetPC());
            var pcStructure = DataService.PCBaseStructure.GetByID(data.StructureID);
            var pcBase = DataService.PCBase.GetByID(pcStructure.PCBaseID);
            var ownerPlayerID = pcBase.PlayerID;

            switch (responseID)
            {
                case 1: // Can Enter Building
                    TogglePermission(ownerPlayerID, StructurePermission.CanEnterBuilding, true);
                    break;
            }

            BuildPublicPermissionsPage();
        }

        public override void EndDialog()
        {
            BaseService.ClearPlayerTempData(GetPC());
        }
    }
}
