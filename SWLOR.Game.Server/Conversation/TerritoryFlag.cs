using System;
using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using SWLOR.Game.Server.ValueObject.Structure;

namespace SWLOR.Game.Server.Conversation
{
    public class TerritoryFlag : ConversationBase
    {
        private class Model
        {
            public NWPlaceable FlagMarker { get; set; }
            public int FlagID { get; set; }
            public bool IsConfirmingTerritoryRaze { get; set; }
            public string TransferPlayerID { get; set; }
            public bool IsConfirmingTransferTerritory { get; set; }
            public string ActivePermissionsPlayerID { get; set; }
            public bool IsAddingPermission { get; set; }
        }

        private readonly IStructureService _structure;
        private readonly IColorTokenService _color;
        private readonly IPlayerService _player;

        public TerritoryFlag(
            INWScript script,
            IDialogService dialog,
            IStructureService structure,
            IColorTokenService color,
            IPlayerService player)
            : base(script, dialog)
        {
            _structure = structure;
            _color = color;
            _player = player;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                    "<SET LATER>",
                    "Manage Permissions",
                    "Transfer Territory Ownership",
                    "Toggle Owner Name",
                    _color.Red("Raze Territory")
            );

            DialogPage managePermissionsPage = new DialogPage(
                    _color.Green("Manage Permissions") + "\n\nPlease select an option.",
                    "Change Build Privacy",
                    "Change Player Permissions",
                    "Back"
            );

            DialogPage changeBuildPrivacyPage = new DialogPage(
                    "<SET LATER>",
                    "Set Privacy: Owner Only",
                    "Set Privacy: Friends Only",
                    "Set Privacy: Public",
                    "Back"
            );

            DialogPage playerPermissionsPage = new DialogPage(
                    _color.Green("Manage Player Permissions") + "\n\n" +
                            "Permissions enable other players to perform actions in this territory.\n\n" +
                            "If you use this feature please be sure to set the territory's privacy setting to " +
                            "'Friends Only' or else the changes you make here won't have any effect.\n\n" +
                            "Permissions set on a territory affect all structures and buildings within the territory. " +
                            "Permissions set on a building affect all structures within that individual building."
            );

            DialogPage managePlayerPermissionsPage = new DialogPage(
                    "<SET LATER>"
            );
            DialogPage addPlayerPermissionList = new DialogPage(
                    "Please select a user you to which you wish to give permissions."
            );

            DialogPage razeTerritoryPage = new DialogPage(
                    _color.Red("WARNING: ") + "You have chosen to raze this entire territory. This will delete all structures, construction sites, stored items and the territory marker permanently.\n\n" +
                            "This action cannot be undone! Are you sure you want to raze this territory?",
                    _color.Red("Confirm Raze Territory"),
                    "Back"
            );

            DialogPage transferOwnershipPage = new DialogPage(
                    _color.Red("WARNING: ") + "You are about to transfer this territory and all of the structures tied to it to another player. " +
                            "All items in containers will be transferred and the friends list will be reset.\n\n" +
                            "Please select a player from the list below to begin the transfer."
            );

            DialogPage confirmTransferOwnershipPage = new DialogPage(
                    "<SET LATER>",
                    "Confirm Transfer Territory",
                    "Back"
            );

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("ManagePermissionsPage", managePermissionsPage);
            dialog.AddPage("PlayerPermissionsPage", playerPermissionsPage);
            dialog.AddPage("ManagePlayerPermissionsPage", managePlayerPermissionsPage);
            dialog.AddPage("AddPlayerPermissionsListPage", addPlayerPermissionList);
            dialog.AddPage("ChangeBuildPrivacyPage", changeBuildPrivacyPage);
            dialog.AddPage("RazeTerritoryPage", razeTerritoryPage);
            dialog.AddPage("TransferOwnershipPage", transferOwnershipPage);
            dialog.AddPage("ConfirmTransferOwnershipPage", confirmTransferOwnershipPage);
            return dialog;
        }

        public override void Initialize()
        {
            Model model = GetDialogCustomData<Model>();

            // Buildings - get flag ID from the area
            if (GetDialogTarget().GetLocalInt("IS_BUILDING_DOOR") == 1)
            {
                int structureID = _structure.GetPlaceableStructureID((NWPlaceable)GetDialogTarget());
                if (structureID <= 0)
                {
                    model.FlagID = GetDialogTarget().Area.GetLocalInt("TERRITORY_FLAG_ID");
                }
                else
                {
                    var flag = _structure.GetPCTerritoryFlagByBuildingStructureID(structureID);
                    model.FlagID = flag.PCTerritoryFlagID;
                }

                // Razing is disabled for buildings
                SetResponseVisible("MainPage", 4, false); // Raze territory
            }
            // Regular territories - get flag ID from the territory flag object
            else
            {
                int flagID = _structure.GetTerritoryFlagID(GetDialogTarget());
                model.FlagID = flagID;
                model.FlagMarker = (NWPlaceable)GetDialogTarget();
            }

            SetDialogCustomData(model);
            BuildMainPageHeader();
        }

        private void BuildMainPageHeader()
        {
            Model model = GetDialogCustomData<Model>();
            int flagID = model.FlagID;
            PCTerritoryFlag flag = _structure.GetPCTerritoryFlagByID(flagID);
            TerritoryStructureCount counts = _structure.GetNumberOfStructuresInTerritory(flagID);

            var blueprint = flag.StructureBlueprint;
            string header = _color.Green("Territory Management Menu") + "\n\n"
                            + _color.Green("Vanity Slots: ") + counts.VanityCount + " / " + blueprint.VanityCount + "\n"
                            + _color.Green("Special Slots: ") + counts.SpecialCount + " / " + blueprint.SpecialCount + "\n"
                            + _color.Green("Resource Slots: ") + counts.ResourceCount + " / " + blueprint.ResourceCount + "\n"
                            + _color.Green("Building Slots: ") + counts.BuildingCount + " / " + blueprint.BuildingCount + "\n"
                            + "Please select an option.";
            SetPageHeader("MainPage", header);
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    HandleMainPageResponse(responseID);
                    break;
                case "ManagePermissionsPage":
                    HandleManagePermissionsResponse(responseID);
                    break;
                case "PlayerPermissionsPage":
                    HandlePlayerPermissionsResponse(responseID);
                    break;
                case "ManagePlayerPermissionsPage":
                    HandleManageUserPermissionsResponse(responseID);
                    break;
                case "AddPlayerPermissionsListPage":
                    HandleAddPlayerPermissionsListResponse(responseID);
                    break;
                case "ChangeBuildPrivacyPage":
                    HandleChangeBuildPrivacyResponse(responseID);
                    break;
                case "RazeTerritoryPage":
                    HandleRazeTerritoryResponse(responseID);
                    break;
                case "TransferOwnershipPage":
                    HandleTransferOwnershipResponse(responseID);
                    break;
                case "ConfirmTransferOwnershipPage":
                    HandleConfirmTransferOwnershipResponse(responseID);
                    break;
            }
        }

        public override void EndDialog()
        {
        }

        private string BuildChangePrivacyHeader()
        {
            Model model = GetDialogCustomData<Model>();
            PCTerritoryFlag entity = _structure.GetPCTerritoryFlagByID(model.FlagID);

            string header = _color.Green("Current Build Privacy: ") + entity.BuildPrivacyDomain.Name + "\n\n";
            header += _color.Green("Owner Only: ") + "Only the owner of this territory may build and manipulate structures at this territory.\n";
            header += _color.Green("Friends Only: ") + "Only you and the people you mark as friends may build and manipulate structures at this territory.\n";
            header += _color.Green("Public: ") + "Anyone may build and manipulate structures at this territory.";

            return header;
        }

        private void HandleMainPageResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Manage Permissions
                    ChangePage("ManagePermissionsPage");
                    break;
                case 2: // Transfer Ownership
                    LoadTransferOwnershipResponses();
                    ChangePage("TransferOwnershipPage");
                    break;
                case 3: // Toggle owner name
                    ToggleOwnerName();
                    break;
                case 4: // Raze Territory
                    ChangePage("RazeTerritoryPage");
                    break;
            }
        }

        private void ToggleOwnerName()
        {
            Model model = GetDialogCustomData<Model>();
            PCTerritoryFlag flag = _structure.GetPCTerritoryFlagByID(model.FlagID);

            flag.ShowOwnerName = !flag.ShowOwnerName;

            if (flag.ShowOwnerName)
            {
                PlayerCharacter owner = _player.GetPlayerEntity(flag.PlayerID);

                // Building
                if (model.FlagMarker == null)
                {
                    GetPC().FloatingText("Now displaying owner's name on this building.");
                }
                // Territory Marker
                else
                {
                    GetPC().FloatingText("Now displaying owner's name on this territory marker.");
                    model.FlagMarker.Name = owner.CharacterName + "'s Territory";
                }

            }
            else
            {
                if (model.FlagMarker == null)
                {
                    GetPC().FloatingText("No longer displaying owner's name on this building.");
                }
                else
                {
                    GetPC().FloatingText("No longer displaying owner's name on this territory marker.");
                    model.FlagMarker.Name = "Claimed Territory";
                }
            }

            _structure.SaveChanges();
            BuildMainPageHeader();
        }

        private void HandleManagePermissionsResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Change Build Privacy
                    SetPageHeader("ChangeBuildPrivacyPage", BuildChangePrivacyHeader());
                    ChangePage("ChangeBuildPrivacyPage");
                    break;
                case 2: // Change Player Permissions
                    BuildChangePlayerPermissionsResponses();
                    ChangePage("PlayerPermissionsPage");
                    break;
                case 3: // Back
                    ChangePage("MainPage");
                    break;
            }
        }

        private void BuildChangePlayerPermissionsResponses()
        {
            Model model = GetDialogCustomData<Model>();
            List<PCTerritoryFlagsPermission> permissions = _structure.GetPermissionsByFlagID(model.FlagID);
            List<string> addedKeys = new List<string>();
            ClearPageResponses("PlayerPermissionsPage");

            AddResponseToPage("PlayerPermissionsPage", _color.Green("Add Player"));

            foreach (PCTerritoryFlagsPermission perm in permissions)
            {
                if (!addedKeys.Contains(perm.PlayerID))
                {
                    addedKeys.Add(perm.PlayerID);
                    AddResponseToPage("PlayerPermissionsPage", "Manage Permissions: " + perm.PlayerCharacter.CharacterName, true, new Tuple<string, dynamic>(string.Empty, perm.PlayerID));
                }
            }

            AddResponseToPage("PlayerPermissionsPage", "Back");
        }

        private void HandlePlayerPermissionsResponse(int responseID)
        {
            DialogResponse response = GetResponseByID("PlayerPermissionsPage", responseID);
            Model model = GetDialogCustomData<Model>();

            switch (responseID)
            {
                case 1: // Add Player
                    model.IsAddingPermission = true;
                    BuildAddPlayerPermissionsListResponses();
                    ChangePage("AddPlayerPermissionsListPage");
                    break;
                default:
                    if (!response.HasCustomData)
                    {
                        ChangePage("ManagePermissionsPage");
                    }
                    else
                    {
                        model.IsAddingPermission = false;
                        model.ActivePermissionsPlayerID = (string)response.CustomData[string.Empty];
                        BuildManagePlayerPermissionsHeader();
                        BuildManagePlayerPermissionsResponses();
                        ChangePage("ManagePlayerPermissionsPage");
                    }

                    break;
            }
        }

        private void BuildAddPlayerPermissionsListResponses()
        {
            Model model = GetDialogCustomData<Model>();
            ClearPageResponses("AddPlayerPermissionsListPage");
            List<PCTerritoryFlagsPermission> existingPermissions = _structure.GetPermissionsByFlagID(model.FlagID);
            List<string> existingUUIDs = new List<string>();

            foreach (PCTerritoryFlagsPermission perm in existingPermissions)
            {
                if (!existingUUIDs.Contains(perm.PlayerID))
                {
                    existingUUIDs.Add(perm.PlayerID);
                }
            }

            NWPlayer player = NWPlayer.Wrap(_.GetFirstPC());
            while (player.IsValid)
            {
                if (!Equals(player, GetPC()) && !player.IsDM)
                {
                    string message = "Add Permissions: ";
                    if (existingUUIDs.Contains(player.GlobalID))
                    {
                        message = "Manage Permissions: ";
                    }

                    AddResponseToPage("AddPlayerPermissionsListPage", message + player.Name, true, new Tuple<string, dynamic>(string.Empty, player.GlobalID));
                }
                player = NWPlayer.Wrap(_.GetNextPC());
            }

            AddResponseToPage("AddPlayerPermissionsListPage", "Back");
        }

        private void HandleAddPlayerPermissionsListResponse(int responseID)
        {
            DialogResponse response = GetResponseByID("AddPlayerPermissionsListPage", responseID);
            Model model = GetDialogCustomData<Model>();

            if (!response.HasCustomData)
            {
                BuildChangePlayerPermissionsResponses();
                ChangePage("PlayerPermissionsPage");
            }
            else
            {
                model.ActivePermissionsPlayerID = (string)response.CustomData[string.Empty];
                BuildManagePlayerPermissionsHeader();
                BuildManagePlayerPermissionsResponses();
                ChangePage("ManagePlayerPermissionsPage");
            }
        }

        private void BuildManagePlayerPermissionsHeader()
        {
            Model model = GetDialogCustomData<Model>();
            PlayerCharacter playerEntity = _player.GetPlayerEntity(model.ActivePermissionsPlayerID);
            List<PCTerritoryFlagsPermission> permissions = _structure.GetPermissionsByPlayerID(model.ActivePermissionsPlayerID, model.FlagID);

            string header = _color.Green("Manage Player Permissions") + "\n\n";
            header += _color.Green("Player: ") + playerEntity.CharacterName + "\n\n";
            header += _color.Green("Current Permissions:\n\n");

            if (permissions == null || permissions.Count <= 0)
                header += "No permissions have been set for this player.";
            else
            {
                foreach (PCTerritoryFlagsPermission perm in permissions)
                {
                    header += perm.TerritoryFlagPermission.Name + "\n";
                }
            }

            SetPageHeader("ManagePlayerPermissionsPage", header);
        }

        private bool DoesPlayerPermissionsContainPermission(List<PCTerritoryFlagsPermission> perms, TerritoryFlagPermission searchPerm)
        {
            if (perms == null) return false;

            foreach (PCTerritoryFlagsPermission perm in perms)
            {
                if (perm.TerritoryFlagPermissionID == searchPerm.TerritoryFlagPermissionID)
                {
                    return true;
                }
            }

            return false;
        }

        private void BuildManagePlayerPermissionsResponses()
        {
            Model model = GetDialogCustomData<Model>();
            List<PCTerritoryFlagsPermission> playerPermissions = _structure.GetPermissionsByPlayerID(model.ActivePermissionsPlayerID, model.FlagID);
            List<TerritoryFlagPermission> permissions = _structure.GetAllTerritorySelectablePermissions();

            ClearPageResponses("ManagePlayerPermissionPage");

            foreach (TerritoryFlagPermission perm in permissions)
            {
                bool hasPermission = DoesPlayerPermissionsContainPermission(playerPermissions, perm);

                if (hasPermission)
                {
                    AddResponseToPage("ManagePlayerPermissionPage", "Remove Permission: " + perm.Name, true, new Tuple<string, dynamic>(string.Empty, perm.TerritoryFlagPermissionID));
                }
                else
                {
                    AddResponseToPage("ManagePlayerPermissionPage", "Add Permission: " + perm.Name, true, new Tuple<string, dynamic>(string.Empty, perm.TerritoryFlagPermissionID));
                }

            }

            AddResponseToPage("ManagePlayerPermissionPage", "Back");
        }

        private void HandleManageUserPermissionsResponse(int responseID)
        {
            Model model = GetDialogCustomData<Model>();
            DialogResponse response = GetResponseByID("ManagePlayerPermissionsPage", responseID);

            if (!response.HasCustomData)
            {
                if (model.IsAddingPermission)
                {
                    ChangePage("AddPlayerPermissionsListPage");
                }
                else
                {
                    BuildChangePlayerPermissionsResponses();
                    ChangePage("PlayerPermissionsPage");
                }
                return;
            }

            int permissionID = (int)response.CustomData[string.Empty];
            List<PCTerritoryFlagsPermission> pcPermissions = _structure.GetPermissionsByPlayerID(model.ActivePermissionsPlayerID, model.FlagID);
            PCTerritoryFlagsPermission foundPerm = null;

            foreach (PCTerritoryFlagsPermission perm in pcPermissions)
            {
                if (perm.TerritoryFlagPermissionID == permissionID)
                {
                    foundPerm = perm;
                    break;
                }
            }

            _structure.TogglePermissionForPlayer(foundPerm, model.ActivePermissionsPlayerID, permissionID, model.FlagID);

            BuildManagePlayerPermissionsHeader();
            BuildManagePlayerPermissionsResponses();
        }


        private void HandleChangeBuildPrivacyResponse(int responseID)
        {
            switch (responseID)
            {
                case 4:
                    ChangePage("ManagePermissionsPage");
                    break;
                default:
                    Model model = GetDialogCustomData<Model>();

                    _structure.ChangeBuildPrivacy(model.FlagID, responseID);
                    
                    SetPageHeader("ChangeBuildPrivacyPage", BuildChangePrivacyHeader());
                    break;
            }
        }

        private void HandleRazeTerritoryResponse(int responseID)
        {
            Model model = GetDialogCustomData<Model>();
            // Safety check to ensure buildings aren't able to be razed.
            if (model.FlagMarker == null) return;

            switch (responseID)
            {
                case 1: // Confirm / REALLY CONFIRM
                    if (model.IsConfirmingTerritoryRaze)
                    {
                        _structure.RazeTerritory(model.FlagMarker);
                        GetPC().FloatingText(_color.Red("Territory razed!"));
                        EndConversation();
                    }
                    else
                    {
                        model.IsConfirmingTerritoryRaze = true;
                        SetResponseText("RazeTerritoryPage", 1, _color.Red("REALLY CONFIRM RAZE TERRITORY"));
                    }

                    break;
                case 2: // Back
                    model.IsConfirmingTerritoryRaze = false;
                    SetResponseText("RazeTerritoryPage", 1, _color.Red("Confirm Raze Territory"));
                    ChangePage("MainPage");
                    break;
            }
        }


        private void LoadTransferOwnershipResponses()
        {
            ClearPageResponses("TransferOwnershipPage");

            NWPlayer player = NWPlayer.Wrap(_.GetFirstPC());
            while (player.IsValid)
            {
                if (!Equals(player, GetPC()) && !player.IsDM)
                {
                    AddResponseToPage("TransferOwnershipPage", "Transfer Ownership: " + player.Name, true, new Tuple<string, dynamic>(string.Empty, player.GlobalID));
                }

                player = NWPlayer.Wrap(_.GetNextPC());
            }

            AddResponseToPage("TransferOwnershipPage", "Back");
        }

        private void HandleTransferOwnershipResponse(int responseID)
        {
            DialogResponse response = GetResponseByID("TransferOwnershipPage", responseID);
            if (!response.HasCustomData)
            {
                ChangePage("MainPage");
                return;
            }

            Model model = GetDialogCustomData<Model>();
            string playerID = (string)response.CustomData[string.Empty];
            model.TransferPlayerID = playerID;
            BuildConfirmTransferOwnershipHeader();
            ChangePage("ConfirmTransferOwnershipPage");
        }

        private void BuildConfirmTransferOwnershipHeader()
        {
            Model model = GetDialogCustomData<Model>();
            PlayerCharacter entity = _player.GetPlayerEntity(model.TransferPlayerID);

            string header = _color.Red("WARNING: ") + "You are about to transfer ownership of this territory. This is your last chance to cancel this action.\n\n";
            header += "This territory will be transferred to the following player: " + _color.Green(entity.CharacterName) + "\n\n";
            header += "Are you sure you want to transfer this territory?";
            SetPageHeader("ConfirmTransferOwnershipPage", header);
        }

        private void HandleConfirmTransferOwnershipResponse(int responseID)
        {
            Model model = GetDialogCustomData<Model>();

            switch (responseID)
            {
                case 1: // Confirm / REALLY CONFIRM
                    if (model.IsConfirmingTransferTerritory)
                    {
                        // Buildings
                        if (model.FlagMarker == null)
                        {
                            _structure.TransferBuildingOwnership(GetDialogTarget().Area, model.TransferPlayerID);
                        }
                        // Territories
                        else
                        {
                            _structure.TransferTerritoryOwnership(model.FlagMarker, model.TransferPlayerID);
                        }

                        EndConversation();
                    }
                    else
                    {
                        model.IsConfirmingTransferTerritory = true;
                        SetResponseText("ConfirmTransferOwnershipPage", 1, "REALLY CONFIRM TRANSFER TERRITORY");
                    }

                    break;
                case 2: // Back
                    SetResponseText("ConfirmTransferOwnershipPage", 1, "Confirm Transfer Territory");
                    model.IsConfirmingTransferTerritory = false;
                    LoadTransferOwnershipResponses();
                    ChangePage("TransferOwnershipPage");
                    break;
            }
        }
    }
}
