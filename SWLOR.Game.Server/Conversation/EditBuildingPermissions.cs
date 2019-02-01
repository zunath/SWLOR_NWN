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

namespace SWLOR.Game.Server.Conversation
{
    public class EditBuildingPermissions: ConversationBase
    {
        private readonly IBaseService _base;
        private readonly IColorTokenService _color;
        private readonly IDataService _data;
        private readonly IBasePermissionService _perm;

        public EditBuildingPermissions(
            INWScript script, 
            IDialogService dialog,
            IBaseService @base,
            IColorTokenService color,
            IDataService data,
            IBasePermissionService perm) 
            : base(script, dialog)
        {
            _base = @base;
            _color = color;
            _data = data;
            _perm = perm;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "Settings adjusted here will only affect this particular structure. If this is a building, the permissions will affect everything inside the building.",
                "Change Player Permissions",
                "Change Public Permissions");

            DialogPage playerListPage = new DialogPage("Please select a player.");
            DialogPage playerDetailsPage = new DialogPage();
            DialogPage publicPermissionsPage = new DialogPage();

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
            var data = _base.GetPlayerTempData(GetPC());

            switch (responseID)
            {
                case 1: // Change Player Permissions
                    if (!_perm.HasStructurePermission(GetPC(), data.StructureID, StructurePermission.CanAdjustPermissions))
                    {
                        GetPC().FloatingText("You do not have permission to change other players' permissions.");
                        return;
                    }

                    BuildPlayerListPage();
                    ChangePage("PlayerListPage");
                    break;
                case 2: // Change Public Permissions
                    if (!_perm.HasStructurePermission(GetPC(), data.StructureID, StructurePermission.CanAdjustPublicPermissions))
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
            ClearPageResponses("PlayerListPage");
            foreach (var player in NWModule.Get().Players)
            {
                if (!Equals(player, GetPC()))
                {
                    AddResponseToPage("PlayerListPage", player.Name, true, player);
                }
            }
        }

        private void PlayerListResponses(int responseID)
        {
            var response = GetResponseByID("PlayerListPage", responseID);
            NWPlayer player = (NWPlayer)response.CustomData;

            BuildPlayerDetailsPage(player);
            ChangePage("PlayerDetailsPage");
        }

        private void BuildPlayerDetailsPage(NWPlayer player)
        {
            ClearPageResponses("PlayerDetailsPage");
            var data = _base.GetPlayerTempData(GetPC());
            var permission = _data.SingleOrDefault<PCBaseStructurePermission>(x => x.PlayerID == player.GlobalID && 
                                                                                   x.PCBaseStructureID == data.StructureID &&
                                                                                   !x.IsPublicPermission);

            // Intentionally excluded permissions: CanAdjustPermissions, CanCancelLease
            bool canPlaceEditStructures = permission?.CanPlaceEditStructures ?? false;
            bool canAccessStructureInventory = permission?.CanAccessStructureInventory ?? false;
            bool canEnterBuilding = permission?.CanEnterBuilding ?? false;
            bool canAdjustPermissions = permission?.CanAdjustPermissions ?? false;
            bool canRetrieveStructures = permission?.CanRetrieveStructures ?? false;
            bool canRenameStructures = permission?.CanRenameStructures ?? false;
            bool canEditPrimaryResidence = permission?.CanEditPrimaryResidence ?? false;
            bool canRemovePrimaryResidence = permission?.CanRemovePrimaryResidence ?? false;
            bool canChangeStructureMode = permission?.CanChangeStructureMode ?? false;
            bool canAdjustPublicPermissions = permission?.CanAdjustPublicPermissions ?? false;
            bool canFlyStarship = permission?.CanFlyStarship ?? false;

            bool isStarship = player.Area.GetLocalInt("BUILDING_TYPE") == (int)Enumeration.BuildingType.Starship;

            string header = _color.Green("Name: ") + player.Name + "\n\n";

            header += _color.Green("Permissions:\n\n");
            header += "Can Place/Edit Structures: " + (canPlaceEditStructures ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Access Structure Inventory: " + (canAccessStructureInventory ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Enter Building: " + (canEnterBuilding ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Adjust Permissions: " + (canAdjustPermissions ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Retrieve Structures: " + (canRetrieveStructures ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Rename Structures: " + (canRenameStructures ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Edit Primary Residence: " + (canEditPrimaryResidence ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Remove Primary Residence: " + (canRemovePrimaryResidence ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Change Structure Mode: " + (canChangeStructureMode ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Adjust PUBLIC Permissions: " + (canAdjustPublicPermissions ? _color.Green("YES") : _color.Red("NO")) + "\n";

            if (isStarship) header += "Can Fly Starship: " + (canFlyStarship ? _color.Green("YES") :_color.Red("NO")) + "\n";

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
            // Add new non-conditional responses here to avoid confusing the response handling logic.
            if (isStarship) AddResponseToPage("PlayerDetailsPage", "Toggle: Can Fly Starship", true, player);
        }

        private void PlayerDetailsResponses(int responseID)
        {
            var response = GetResponseByID("PlayerDetailsPage", responseID);
            NWPlayer player = (NWPlayer) response.CustomData;
            Guid playerID = player.GlobalID;

            switch (responseID)
            {
                case 1: // Can Place/Edit Structures
                    TogglePermission(playerID, StructurePermission.CanPlaceEditStructures, false);
                    break;
                case 2: // Can Access Structure Inventory
                    TogglePermission(playerID, StructurePermission.CanAccessStructureInventory, false);
                    break;
                case 3: // Can Enter Building
                    TogglePermission(playerID, StructurePermission.CanEnterBuilding, false);
                    break;
                case 4: // Can Adjust Permissions
                    TogglePermission(playerID, StructurePermission.CanAdjustPermissions, false);
                    break;
                case 5: // Can Retrieve Structures
                    TogglePermission(playerID, StructurePermission.CanRetrieveStructures, false);
                    break;
                case 6: // Can Rename Structures
                    TogglePermission(playerID, StructurePermission.CanRenameStructures, false);
                    break;
                case 7: // Can Edit Primary Residence
                    TogglePermission(playerID, StructurePermission.CanEditPrimaryResidence, false);
                    break;
                case 8: // Can Remove Primary Residence
                    TogglePermission(playerID, StructurePermission.CanRemovePrimaryResidence, false);
                    break;
                case 9: // Can Change Structure Mode
                    TogglePermission(playerID, StructurePermission.CanChangeStructureMode, false);
                    break;
                case 10: // Can Adjust PUBLIC Permissions
                    TogglePermission(playerID, StructurePermission.CanAdjustPublicPermissions, false);
                    break;
                case 11: // Can fly starship
                    TogglePermission(playerID, StructurePermission.CanFlyStarship, false);
                    break;
            }

            BuildPlayerDetailsPage(player);
        }

        private void TogglePermission(Guid playerID, StructurePermission permission, bool isPublicPermission)
        {
            var data = _base.GetPlayerTempData(GetPC());
            var dbPermission = isPublicPermission ? 
                _data.SingleOrDefault<PCBaseStructurePermission>(x => x.PCBaseStructureID == data.StructureID &&
                                                                      x.IsPublicPermission) :
                _data.SingleOrDefault<PCBaseStructurePermission>(x => x.PlayerID == playerID && 
                                                                      x.PCBaseStructureID == data.StructureID &&
                                                                      !x.IsPublicPermission);
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

            _data.SubmitDataChange(dbPermission, action);
        }

        private void BuildPublicPermissionsPage()
        {
            ClearPageResponses("PublicPermissionsPage");
            var data = _base.GetPlayerTempData(GetPC());
            var permission = _data.SingleOrDefault<PCBaseStructurePermission>(x => x.PCBaseStructureID == data.StructureID &&
                                                                                   x.IsPublicPermission);

            // Intentionally excluded permissions:
            // CanAdjustPermissions, CanCancelLease, CanPlaceEditStructures, CanAccessStructureInventory, CanAdjustPermissions,
            // CanRetrieveStructures, CanRenameStructures, CanEditPrimaryResidence, CanRemovePrimaryResidence, CanChangeStructureMode,
            // CanAdjustPublicPermissions
            bool canEnterBuilding = permission?.CanEnterBuilding ?? false;

            string header = _color.Green("Public Permissions: ") + "\n\n";
            header += "Can Enter Building: " + (canEnterBuilding ? _color.Green("YES") : _color.Red("NO")) + "\n";

            SetPageHeader("PublicPermissionsPage", header);

            AddResponseToPage("PublicPermissionsPage", "Toggle: Can Enter Building");
        }

        private void PublicPermissionsResponses(int responseID)
        {
            var data = _base.GetPlayerTempData(GetPC());
            var pcStructure = _data.Get<PCBaseStructure>(data.StructureID);
            var pcBase = _data.Get<PCBase>(pcStructure.PCBaseID);
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
            _base.ClearPlayerTempData(GetPC());
        }
    }
}
