using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class AdjustLighting : ConversationBase
    {

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "Adjust Area Lighting.",
                "Main Light 1",
                "Main Light 2",
                "Source Light 1",
                "Source Light 2");

            DialogPage colorPage = new DialogPage("Please select a color.");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("ColorPage", colorPage);
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
                case "ColorPage":
                    ColorPageResponses(responseID);
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {

        }

        private void MainResponses(int responseID)
        {
            DialogPage currentPage = GetCurrentPage();
            currentPage.CustomData.Clear();
            currentPage.CustomData.Add("LIGHT_TYPE", responseID);
            BuildColorPage(responseID);
            ChangePage("ColorPage");
        }

        private void BuildColorPage(int responseID)
        {
            List<string []> colorList = new List<string []>();            
            
            ClearPageResponses("ColorPage");


            //responseIDs:
            //case 1: // Change Main Light 1
            //case 2: // Change Main Light 2
            //case 3: // Change Source Light 1
            //case 4: // Change Source Light 2

            // TILE_MAIN_LIGHT_* Constant Group
            if (responseID == 1 || responseID == 2)
            {
                colorList.Add(new string[2] { "Black", "0" });
                colorList.Add(new string[2] { "Dim White", "1" });
                colorList.Add(new string[2] { "White", "2" });
                colorList.Add(new string[2] { "Bright White", "3" });
                colorList.Add(new string[2] { "Pale Dark Yellow", "4" });
                colorList.Add(new string[2] { "Dark Yellow", "5" });
                colorList.Add(new string[2] { "Pale Yellow", "6" });
                colorList.Add(new string[2] { "Yellow", "7" });
                colorList.Add(new string[2] { "Pale Dark Green", "8" });
                colorList.Add(new string[2] { "Dark Green", "9" });
                colorList.Add(new string[2] { "Pale Green", "10" });
                colorList.Add(new string[2] { "Green", "11" });
                colorList.Add(new string[2] { "Pale Dark Aqua", "12" });
                colorList.Add(new string[2] { "Dark Aqua", "13" });
                colorList.Add(new string[2] { "Pale Aqua", "14" });
                colorList.Add(new string[2] { "Aqua", "15" });
                colorList.Add(new string[2] { "Pale Dark Blue", "16" });
                colorList.Add(new string[2] { "Dark Blue", "17" });
                colorList.Add(new string[2] { "Pale Blue", "18" });
                colorList.Add(new string[2] { "Blue", "19" });
                colorList.Add(new string[2] { "Pale Dark Purple", "20" });
                colorList.Add(new string[2] { "Dark Purple", "21" });
                colorList.Add(new string[2] { "Pale Purple", "22" });
                colorList.Add(new string[2] { "Purple", "23" });
                colorList.Add(new string[2] { "Pale Dark Red", "24" });
                colorList.Add(new string[2] { "Dark Red", "25" });
                colorList.Add(new string[2] { "Pale Red", "26" });
                colorList.Add(new string[2] { "Red", "27" });
                colorList.Add(new string[2] { "Pale Dark Orange", "28" });
                colorList.Add(new string[2] { "Dark Orange", "29" });
                colorList.Add(new string[2] { "Pale Orange", "30" });
                colorList.Add(new string[2] { "Orange", "31" });
            }
            //TILE_SOURCE_LIGHT_* Constant Group
            else if (responseID == 3 || responseID == 4)
            {
                colorList.Add(new string[2] { "Black", "0" });
                colorList.Add(new string[2] { "White", "1" });
                colorList.Add(new string[2] { "Pale Dark Yellow", "2" });
                colorList.Add(new string[2] { "Pale Yellow", "3" });
                colorList.Add(new string[2] { "Pale Dark Green", "4" });
                colorList.Add(new string[2] { "Pale Green", "5" });
                colorList.Add(new string[2] { "Pale Dark Aqua", "6" });
                colorList.Add(new string[2] { "Pale Aqua", "7" });
                colorList.Add(new string[2] { "Pale Dark Blue", "8" });
                colorList.Add(new string[2] { "Pale Blue", "9" });
                colorList.Add(new string[2] { "Pale Dark Purple", "10" });
                colorList.Add(new string[2] { "Pale Purple", "11" });
                colorList.Add(new string[2] { "Pale Dark Red", "12" });
                colorList.Add(new string[2] { "Pale Red", "13" });
                colorList.Add(new string[2] { "Pale Dark Orange", "14" });
                colorList.Add(new string[2] { "Pale Orange", "15" });
            }

            foreach (string[] color in colorList)
            {
                //Console.WriteLine("Adding Color: " + color[0] + " to page with index " + color[1]);
                AddResponseToPage("ColorPage", color[0], true, color[1]);
            }
        }

        private void ColorPageResponses(int responseID)
        {
            DialogPage mainPage = GetPageByName("MainPage");
            int lightType = mainPage.CustomData.GetValueOrDefault("LIGHT_TYPE");
            var response = GetResponseByID("ColorPage", responseID);

            //Console.WriteLine("Light Type: " + lightType);
            //Console.WriteLine("ResponseID: " + responseID);
            //Console.WriteLine("New Color Index: " + Int32.Parse(response.CustomData.ToString()));

            // Setup placement grid                
            NWArea area = _.GetArea(GetPC());
            Vector vPos;
            vPos.X = 0.0f;
            vPos.Y = 0.0f;
            vPos.Z = 0.0f;
            for (int i = 0; i <= area.Height; i++)
            {
                vPos.X = (float)i;
                for (int j = 0; j <= area.Width; j++)
                {
                    vPos.Y = (float)j;
                    
                    Location location = _.Location(area, vPos, 0.0f);

                    //Console.WriteLine("Setting Tile Color: X = " + vPos.X + " Y = " + vPos.Y);
                    switch (lightType)
                    {
                        case 1: // Change Main Light 1
                            _.SetTileMainLightColor(location, Int32.Parse(response.CustomData.ToString()), _.GetTileMainLight2Color(location));
                            break;
                        case 2: // Change Main Light 2
                            _.SetTileMainLightColor(location, _.GetTileMainLight1Color(location), Int32.Parse(response.CustomData.ToString()));
                            break;
                        case 3: // Change Source Light 1
                            _.SetTileSourceLightColor(location, Int32.Parse(response.CustomData.ToString()), _.GetTileSourceLight2Color(location));
                            break;
                        case 4: // Change Source Light 2
                            _.SetTileSourceLightColor(location, _.GetTileSourceLight1Color(location), Int32.Parse(response.CustomData.ToString()));
                            break;
                    }
                }
            }
            _.RecomputeStaticLighting(area);
            var data = BaseService.GetPlayerTempData(GetPC());
            int buildingTypeID = data.TargetArea.GetLocalInt("BUILDING_TYPE");
            Enumeration.BuildingType buildingType = buildingTypeID <= 0 ? Enumeration.BuildingType.Exterior : (Enumeration.BuildingType)buildingTypeID;
            data.BuildingType = buildingType;

            if (buildingType == Enumeration.BuildingType.Apartment)
            {
                Guid pcBaseID = new Guid(data.TargetArea.GetLocalString("PC_BASE_ID"));
                var pcBase = DataService.PCBase.GetByID(pcBaseID);

                switch (lightType)
                {
                    case 1: // Change Main Light 1
                        pcBase.TileMainLight1Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 2: // Change Main Light 2
                        pcBase.TileMainLight2Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 3: // Change Source Light 1
                        pcBase.TileSourceLight1Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 4: // Change Source Light 2
                        pcBase.TileSourceLight2Color = Int32.Parse(response.CustomData.ToString());
                        break;
                }

                DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);
            }
            else if (buildingType == Enumeration.BuildingType.Interior)
            {
                Guid pcBaseStructureID = new Guid(data.TargetArea.GetLocalString("PC_BASE_STRUCTURE_ID"));
                var structure = DataService.PCBaseStructure.GetByID(pcBaseStructureID);

                switch (lightType)
                {
                    case 1: // Change Main Light 1
                        structure.TileMainLight1Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 2: // Change Main Light 2
                        structure.TileMainLight2Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 3: // Change Source Light 1
                        structure.TileSourceLight1Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 4: // Change Source Light 2
                        structure.TileSourceLight2Color = Int32.Parse(response.CustomData.ToString());
                        break;
                }

                DataService.SubmitDataChange(structure, DatabaseActionType.Update);
            }
            else if (buildingType == Enumeration.BuildingType.Starship)
            {
                // Note - starships need to record in both the base and the structure entries.
                Guid pcBaseStructureID = new Guid(data.TargetArea.GetLocalString("PC_BASE_STRUCTURE_ID"));
                var structure = DataService.PCBaseStructure.GetByID(pcBaseStructureID);
                var pcBase = DataService.PCBase.GetByID(structure.PCBaseID);

                switch (lightType)
                {
                    case 1: // Change Main Light 1
                        structure.TileMainLight1Color = Int32.Parse(response.CustomData.ToString());
                        pcBase.TileMainLight1Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 2: // Change Main Light 2
                        structure.TileMainLight2Color = Int32.Parse(response.CustomData.ToString());
                        pcBase.TileMainLight2Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 3: // Change Source Light 1
                        structure.TileSourceLight1Color = Int32.Parse(response.CustomData.ToString());
                        pcBase.TileSourceLight1Color = Int32.Parse(response.CustomData.ToString());
                        break;
                    case 4: // Change Source Light 2
                        structure.TileSourceLight2Color = Int32.Parse(response.CustomData.ToString());
                        pcBase.TileSourceLight2Color = Int32.Parse(response.CustomData.ToString());
                        break;
                }
                
                DataService.SubmitDataChange(structure, DatabaseActionType.Update);               
                DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);
            }
            
            BuildColorPage(lightType);
        }

        private void BuildPlayerDetailsPage(Player player)
        {
            ClearPageResponses("PlayerDetailsPage");
            var data = BaseService.GetPlayerTempData(GetPC());
            var permission = DataService.PCBasePermission.GetPlayerPrivatePermissionOrDefault(player.ID, data.PCBaseID);

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
            bool canDockShip = permission?.CanDockStarship ?? false;
            bool canAdjustPublicPermissions = permission?.CanAdjustPublicPermissions ?? false;

            string header = ColorTokenService.Green("Name: ") + player.CharacterName + "\n\n";

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
            Player player = (Player)response.CustomData;

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

            DatabaseActionType action = DatabaseActionType.Update;
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
            bool canEnterBuildings = permission?.CanEnterBuildings ?? false;
            bool canDockStarship = permission?.CanDockStarship ?? false;

            string header = ColorTokenService.Green("Public Permissions: ") + "\n\n";
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
