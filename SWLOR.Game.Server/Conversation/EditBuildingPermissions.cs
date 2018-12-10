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
                "Change Permissions");

            DialogPage playerListPage = new DialogPage("Please select a player.");

            DialogPage playerDetailsPage = new DialogPage();

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("PlayerListPage", playerListPage);
            dialog.AddPage("PlayerDetailsPage", playerDetailsPage);
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
            switch (responseID)
            {
                case 1: // Change Permissions
                    var data = _base.GetPlayerTempData(GetPC());
                    if (!_perm.HasStructurePermission(GetPC(), data.StructureID, StructurePermission.CanAdjustPermissions))
                    {
                        GetPC().FloatingText("You do not have permission to change other players' permissions.");
                        return;
                    }

                    BuildPlayerListPage();
                    ChangePage("PlayerListPage");
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
        }

        private void PlayerDetailsResponses(int responseID)
        {
            var response = GetResponseByID("PlayerDetailsPage", responseID);
            NWPlayer player = (NWPlayer) response.CustomData;

            switch (responseID)
            {
                case 1: // Can Place/Edit Structures
                    TogglePermission(player, StructurePermission.CanPlaceEditStructures);
                    break;
                case 2: // Can Access Structure Inventory
                    TogglePermission(player, StructurePermission.CanAccessStructureInventory);
                    break;
                case 3: // Can Enter Building
                    TogglePermission(player, StructurePermission.CanEnterBuilding);
                    break;
                case 4: // Can Adjust Permissions
                    TogglePermission(player, StructurePermission.CanAdjustPermissions);
                    break;
                case 5: // Can Retrieve Structures
                    TogglePermission(player, StructurePermission.CanRetrieveStructures);
                    break;
                case 6: // Can Rename Structures
                    TogglePermission(player, StructurePermission.CanRenameStructures);
                    break;
                case 7: // Can Edit Primary Residence
                    TogglePermission(player, StructurePermission.CanEditPrimaryResidence);
                    break;
                case 8: // Can Remove Primary Residence
                    TogglePermission(player, StructurePermission.CanRemovePrimaryResidence);
                    break;
                case 9: // Can Change Structure Mode
                    TogglePermission(player, StructurePermission.CanChangeStructureMode);
                    break;
                case 10:
                    TogglePermission(player, StructurePermission.CanAdjustPublicPermissions);
                    break;
            }

            BuildPlayerDetailsPage(player);
        }

        private void TogglePermission(NWPlayer player, StructurePermission permission)
        {
            var data = _base.GetPlayerTempData(GetPC());
            var dbPermission = _data.SingleOrDefault<PCBaseStructurePermission>(x => x.PlayerID == player.GlobalID && 
                                                                                     x.PCBaseStructureID == data.StructureID &&
                                                                                     !x.IsPublicPermission);
            var action = DatabaseActionType.Update;

            if (dbPermission == null)
            {
                dbPermission = new PCBaseStructurePermission()
                {
                    PCBaseStructureID = data.StructureID,
                    PlayerID = player.GlobalID
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(permission), permission, null);
            }

            _data.SubmitDataChange(dbPermission, action);
        }

        public override void EndDialog()
        {
            _base.ClearPlayerTempData(GetPC());
        }
    }
}
