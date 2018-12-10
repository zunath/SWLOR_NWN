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
    public class EditBasePermissions: ConversationBase
    {
        private readonly IBaseService _base;
        private readonly IColorTokenService _color;
        private readonly IDataService _data;
        private readonly IBasePermissionService _perm;

        public EditBasePermissions(
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
                "Settings adjusted here will affect your entire base. If you want to adjust individual structures, such as buildings, use their individual menus to do so.",
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
                    if (!_perm.HasBasePermission(GetPC(), data.PCBaseID, BasePermission.CanAdjustPermissions))
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
            var permission = _data.SingleOrDefault<PCBasePermission>(x => x.PlayerID == player.GlobalID && x.PCBaseID == data.PCBaseID);
            
            // Intentionally excluded permissions: CanAdjustPermissions, CanCancelLease
            bool canPlaceEditStructures = permission?.CanPlaceEditStructures ?? false;
            bool canAccessStructureInventory = permission?.CanAccessStructureInventory ?? false;
            bool canManageBaseFuel = permission?.CanManageBaseFuel ?? false;
            bool canExtendLease = permission?.CanExtendLease ?? false;
            bool canEnterBuildings = permission?.CanEnterBuildings ?? false;
            bool canRetrieveStructures = permission?.CanRetrieveStructures ?? false;
            bool canRenameStructures = permission?.CanRenameStructures ?? false;
            bool canEditPrimaryResidence = permission?.CanEditPrimaryResidence ?? false;
            bool canRemovePrimaryResidence = permission?.CanRemovePrimaryResidence ?? false;
            bool canChangeStructureMode = permission?.CanChangeStructureMode ?? false;

            string header = _color.Green("Name: ") + player.Name + "\n\n";

            header += _color.Green("Permissions:\n\n");
            header += "Can Place/Edit Structures: " + (canPlaceEditStructures ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Access Structure Inventory: " + (canAccessStructureInventory ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Manage Base Fuel: " + (canManageBaseFuel ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Extend Lease: " + (canExtendLease ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Enter Buildings: " + (canEnterBuildings ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Retrieve Structures: " + (canRetrieveStructures ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Rename Structures: " + (canRenameStructures ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Edit Primary Residence: " + (canEditPrimaryResidence ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Remove Primary Residence: " + (canRemovePrimaryResidence ? _color.Green("YES") : _color.Red("NO")) + "\n";
            header += "Can Change Structure Mode: " + (canChangeStructureMode ? _color.Green("YES") : _color.Red("NO")) + "\n";

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
        }

        private void PlayerDetailsResponses(int responseID)
        {
            var response = GetResponseByID("PlayerDetailsPage", responseID);
            NWPlayer player = (NWPlayer)response.CustomData;

            switch (responseID)
            {
                case 1: // Can Place/Edit Structures
                    TogglePermission(player, BasePermission.CanPlaceEditStructures);
                    break;
                case 2: // Can Access Structure Inventory
                    TogglePermission(player, BasePermission.CanAccessStructureInventory);
                    break;
                case 3: // Can Manage Base Fuel
                    TogglePermission(player, BasePermission.CanManageBaseFuel);
                    break;
                case 4: // Can Extend Lease
                    TogglePermission(player, BasePermission.CanExtendLease);
                    break;
                case 5: // Can Enter Buildings
                    TogglePermission(player, BasePermission.CanEnterBuildings);
                    break;
                case 6: // Can Retrieve Structures
                    TogglePermission(player, BasePermission.CanRetrieveStructures);
                    break;
                case 7: // Can Rename Structures
                    TogglePermission(player, BasePermission.CanRenameStructures);
                    break;
                case 8: // Can Edit Primary Residence
                    TogglePermission(player, BasePermission.CanEditPrimaryResidence);
                    break;
                case 9: // Can Change Structure Mode
                    TogglePermission(player, BasePermission.CanChangeStructureMode);
                    break;
            }

            BuildPlayerDetailsPage(player);
        }

        private void TogglePermission(NWPlayer player, BasePermission permission)
        {
            var data = _base.GetPlayerTempData(GetPC());
            var dbPermission = _data.SingleOrDefault<PCBasePermission>(x => x.PlayerID == player.GlobalID && x.PCBaseID == data.PCBaseID);

            DatabaseActionType action = DatabaseActionType.Update;
            if (dbPermission == null)
            {
                dbPermission = new PCBasePermission
                {
                    PCBaseID = data.PCBaseID,
                    PlayerID = player.GlobalID
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
