using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class EditBasePermissions: ConversationBase
    {

        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");

            var mainPage = new DialogPage(
                "Settings adjusted here will affect your entire base. If you want to adjust individual structures, such as buildings, use their individual menus to do so.",
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
                case 1: // Change Permissions
                    if (!BasePermissionService.HasBasePermission(GetPC(), data.PCBaseID, BasePermission.CanAdjustPermissions))
                    {
                        GetPC().FloatingText("You do not have permission to change other players' permissions.");
                        return;
                    }

                    BuildPlayerListPage();
                    ChangePage("PlayerListPage");
                    break;
                case 2: // Change Public Permissions
                    var pcBase = DataService.PCBase.GetByID(data.PCBaseID);
                    
                    if (pcBase.Sector == "AP")
                    {
                        GetPC().FloatingText("Public permissions cannot be adjusted inside apartments.");
                        return;
                    }

                    if (!BasePermissionService.HasBasePermission(GetPC(), data.PCBaseID, BasePermission.CanAdjustPublicPermissions))
                    {
                        GetPC().FloatingText("You do not have permission to change this base's public permissions.");
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
            var permission = DataService.PCBasePermission.GetPlayerPrivatePermissionOrDefault(player.ID, data.PCBaseID);
            
            // Intentionally excluded permissions: CanAdjustPermissions, CanCancelLease
            var canPlaceEditStructures = permission?.CanPlaceEditStructures ?? false;
            var canAccessStructureInventory = permission?.CanAccessStructureInventory ?? false;
            var canManageBaseFuel = permission?.CanManageBaseFuel ?? false;
            var canExtendLease = permission?.CanExtendLease ?? false;
            var canEnterBuildings = permission?.CanEnterBuildings ?? false;
            var canRetrieveStructures = permission?.CanRetrieveStructures ?? false;
            var canRenameStructures = permission?.CanRenameStructures ?? false;
            var canEditPrimaryResidence = permission?.CanEditPrimaryResidence ?? false;
            var canRemovePrimaryResidence = permission?.CanRemovePrimaryResidence ?? false;
            var canChangeStructureMode = permission?.CanChangeStructureMode ?? false;
            var canDockShip = permission?.CanDockStarship ?? false;
            var canAdjustPublicPermissions = permission?.CanAdjustPublicPermissions ?? false;

            var header = ColorTokenService.Green("Name: ") + player.CharacterName + "\n\n";
            
            header += ColorTokenService.Green("Permissions:\n\n");
            header += "Can Place/Edit Structures: " + (canPlaceEditStructures ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Access Structure Inventory: " + (canAccessStructureInventory ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Manage Base Fuel: " + (canManageBaseFuel ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Extend Lease: " + (canExtendLease ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Enter Buildings: " + (canEnterBuildings ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Retrieve Structures: " + (canRetrieveStructures ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Rename Structures: " + (canRenameStructures ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Edit Primary Residence: " + (canEditPrimaryResidence ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Remove Primary Residence: " + (canRemovePrimaryResidence ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Change Structure Mode: " + (canChangeStructureMode ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Dock Starships: " + (canDockShip ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Adjust PUBLIC Permissions: " + (canAdjustPublicPermissions ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";

            SetPageHeader("PlayerDetailsPage", header);

            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Place/Edit Structures", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Access Structure Inventory", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Manage Base Fuel", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Extend Lease", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Enter Buildings", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Retrieve Structures", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Rename Structures", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Edit Primary Residence", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Change Structure Mode", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Dock Starships", true, player);
            AddResponseToPage("PlayerDetailsPage", "Toggle: Can Adjust PUBLIC Permissions", true, player);
            AddResponseToPage("PlayerDetailsPage", ColorTokenService.Red("WARNING") + ": Delete Player Permissions", true, player);
        }

        private void PlayerDetailsResponses(int responseID)
        {
            var response = GetResponseByID("PlayerDetailsPage", responseID);
            var player = (Player)response.CustomData;

            switch (responseID)
            {
                case 1: // Can Place/Edit Structures
                    TogglePermission(player.ID, BasePermission.CanPlaceEditStructures, false);
                    break;
                case 2: // Can Access Structure Inventory
                    TogglePermission(player.ID, BasePermission.CanAccessStructureInventory, false);
                    break;
                case 3: // Can Manage Base Fuel
                    TogglePermission(player.ID, BasePermission.CanManageBaseFuel, false);
                    break;
                case 4: // Can Extend Lease
                    TogglePermission(player.ID, BasePermission.CanExtendLease, false);
                    break;
                case 5: // Can Enter Buildings
                    TogglePermission(player.ID, BasePermission.CanEnterBuildings, false);
                    break;
                case 6: // Can Retrieve Structures
                    TogglePermission(player.ID, BasePermission.CanRetrieveStructures, false);
                    break;
                case 7: // Can Rename Structures
                    TogglePermission(player.ID, BasePermission.CanRenameStructures, false);
                    break;
                case 8: // Can Edit Primary Residence
                    TogglePermission(player.ID, BasePermission.CanEditPrimaryResidence, false);
                    break;
                case 9: // Can Change Structure Mode
                    TogglePermission(player.ID, BasePermission.CanChangeStructureMode, false);
                    break;
                case 10: // Can Dock Starships
                    TogglePermission(player.ID, BasePermission.CanDockStarship, false);
                    break;
                case 11: // Can Adjust PUBLIC Permissions
                    TogglePermission(player.ID, BasePermission.CanAdjustPublicPermissions, false);
                    break;
                case 12: // Delete this Players Permissions object
                    DeletePlayerPermission(player.ID, BasePermission.CanAdjustPublicPermissions, false);
                    break;
            }

            BuildPlayerDetailsPage(player);
        }

        private void TogglePermission(Guid playerID, BasePermission permission, bool isPublicPermission)
        {
            var data = BaseService.GetPlayerTempData(GetPC());
            var dbPermission = isPublicPermission ?
                DataService.PCBasePermission.GetPublicPermissionOrDefault(data.PCBaseID) :
                DataService.PCBasePermission.GetPlayerPrivatePermissionOrDefault(playerID, data.PCBaseID);

            var action = DatabaseActionType.Update;
            if (dbPermission == null)
            {
                dbPermission = new PCBasePermission
                {
                    PCBaseID = data.PCBaseID,
                    PlayerID = playerID,
                    IsPublicPermission = isPublicPermission
                };
                action = DatabaseActionType.Insert;
            }

            switch (permission)
            {
                case BasePermission.CanPlaceEditStructures:
                    dbPermission.CanPlaceEditStructures = !dbPermission.CanPlaceEditStructures;
                    break;
                case BasePermission.CanAccessStructureInventory:
                    dbPermission.CanAccessStructureInventory = !dbPermission.CanAccessStructureInventory;
                    break;
                case BasePermission.CanManageBaseFuel:
                    dbPermission.CanManageBaseFuel = !dbPermission.CanManageBaseFuel;
                    break;
                case BasePermission.CanExtendLease:
                    dbPermission.CanExtendLease = !dbPermission.CanExtendLease;
                    break;
                case BasePermission.CanEnterBuildings:
                    dbPermission.CanEnterBuildings = !dbPermission.CanEnterBuildings;
                    break;
                case BasePermission.CanRetrieveStructures:
                    dbPermission.CanRetrieveStructures = !dbPermission.CanRetrieveStructures;
                    break;
                case BasePermission.CanRenameStructures:
                    dbPermission.CanRenameStructures = !dbPermission.CanRenameStructures;
                    break;
                case BasePermission.CanEditPrimaryResidence:
                    dbPermission.CanEditPrimaryResidence = !dbPermission.CanEditPrimaryResidence;
                    break;
                case BasePermission.CanRemovePrimaryResidence:
                    dbPermission.CanRemovePrimaryResidence = !dbPermission.CanRemovePrimaryResidence;
                    break;
                case BasePermission.CanChangeStructureMode:
                    dbPermission.CanChangeStructureMode = !dbPermission.CanChangeStructureMode;
                    break;
                case BasePermission.CanAdjustPublicPermissions:
                    dbPermission.CanAdjustPublicPermissions = !dbPermission.CanAdjustPublicPermissions;
                    break;
                case BasePermission.CanFlyStarship:
                    dbPermission.CanFlyStarship = !dbPermission.CanFlyStarship;
                    break;
                case BasePermission.CanDockStarship:
                    dbPermission.CanDockStarship = !dbPermission.CanDockStarship;
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
            var permission = DataService.PCBasePermission.GetPublicPermissionOrDefault(data.PCBaseID);

            // Intentionally excluded permissions:
            // CanAdjustPermissions, CanCancelLease, CanPlaceEditStructures, CanAccessStructureInventory, CanAdjustPermissions,
            // CanRetrieveStructures, CanRenameStructures, CanEditPrimaryResidence, CanRemovePrimaryResidence, CanChangeStructureMode,
            // CanAdjustPublicPermissions
            var canEnterBuildings = permission?.CanEnterBuildings ?? false;
            var canDockStarship = permission?.CanDockStarship ?? false;

            var header = ColorTokenService.Green("Public Permissions: ") + "\n\n";
            header += "Can Enter Buildings: " + (canEnterBuildings ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";
            header += "Can Dock Starships: " + (canDockStarship ? ColorTokenService.Green("YES") : ColorTokenService.Red("NO")) + "\n";

            SetPageHeader("PublicPermissionsPage", header);

            AddResponseToPage("PublicPermissionsPage", "Toggle: Can Enter Buildings");
            AddResponseToPage("PublicPermissionsPage", "Toggle: Can Dock Starships");
        }

        private void PublicPermissionsResponses(int responseID)
        {
            var data = BaseService.GetPlayerTempData(GetPC());
            var pcBase = DataService.PCBase.GetByID(data.PCBaseID);
            var ownerPlayerID = pcBase.PlayerID;

            switch (responseID)
            {
                case 1: // Can Enter Buildings
                    TogglePermission(ownerPlayerID, BasePermission.CanEnterBuildings, true);
                    break;
                case 2: // Can Dock Starships
                    TogglePermission(ownerPlayerID, BasePermission.CanDockStarship, true);
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
