using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class BuildToolMenu: ConversationBase
    {
        private class Model
        {
            public List<NWPlaceable> NearbyStructures { get; set; }
            public NWPlaceable ActiveStructure { get; set; }
            public bool IsConfirmingRaze { get; set; }
            public Location TargetLocation { get; set; }
            public NWObject Flag { get; set; }
            public bool IsActiveStructureBuilding { get; set; }

            public Model()
            {
                NearbyStructures = new List<NWPlaceable>();
            }
        }

        private readonly IStructureService _structure;
        private readonly IColorTokenService _color;

        public BuildToolMenu(
            INWScript script, 
            IDialogService dialog,
            IStructureService structure,
            IColorTokenService color) 
            : base(script, dialog)
        {
            _structure = structure;
            _color = color;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "Please select an option.\n\nStructures in this list are ordered by distance from the location targeted. "
            );

            DialogPage manipulateStructurePage = new DialogPage(
                "What would you like to do with this structure?",
                "Rotate",
                "Move",
                _color.Red("Raze"),
                "Back"
            );

            DialogPage rotateStructurePage = new DialogPage(
                "Please select a rotation.",
                "Set Facing: East",
                "Set Facing: North",
                "Set Facing: West",
                "Set Facing: South",
                "Rotate 20\u00b0",
                "Rotate 30\u00b0",
                "Rotate 45\u00b0",
                "Rotate 60\u00b0",
                "Rotate 75\u00b0",
                "Rotate 90\u00b0",
                "Rotate 180\u00b0",
                "Back"
            );

            DialogPage razeStructurePage = new DialogPage(
                _color.Red("WARNING: ") +
                "You are about to destroy a structure. All items inside of this structure will be permanently destroyed.\n\n" +
                "Are you sure you want to raze this structure?\n",
                _color.Red("Confirm Raze"),
                "Back"
            );

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("ManipulateStructurePage", manipulateStructurePage);
            dialog.AddPage("RotateStructurePage", rotateStructurePage);
            dialog.AddPage("RazeStructurePage", razeStructurePage);
            return dialog;
        }

        public override void Initialize()
        {
            NWPlayer oPC = GetPC();
            Model model = GetDialogCustomData<Model>();
            model.TargetLocation = oPC.GetLocalLocation("BUILD_TOOL_LOCATION_TARGET");
            oPC.DeleteLocalLocation("BUILD_TOOL_LOCATION_TARGET");
            model.Flag = _structure.GetTerritoryFlagOwnerOfLocation(model.TargetLocation);
            SetDialogCustomData(model);
            BuildMainMenuResponses(null);
        }

        private void ToggleRotateOptions()
        {
            Model model = GetDialogCustomData<Model>();

            bool isVisible = !model.IsActiveStructureBuilding;
            SetResponseVisible("RotateStructurePage", 5, isVisible);
            SetResponseVisible("RotateStructurePage", 6, isVisible);
            SetResponseVisible("RotateStructurePage", 7, isVisible);
            SetResponseVisible("RotateStructurePage", 8, isVisible);
            SetResponseVisible("RotateStructurePage", 9, isVisible);
            SetResponseVisible("RotateStructurePage", 10, isVisible);
            SetResponseVisible("RotateStructurePage", 11, isVisible);
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    HandleMainMenuResponse(responseID);
                    break;
                case "ManipulateStructurePage":
                    switch (responseID)
                    {
                        case 1: // Rotate
                            ToggleRotateOptions();
                            ChangePage("RotateStructurePage");
                            break;
                        case 2: // Move
                            HandleMoveStructure();
                            break;
                        case 3: // Raze
                            ChangePage("RazeStructurePage");
                            break;
                        case 4: // Back
                            BuildMainMenuResponses(null);
                            ChangePage("MainPage");
                            break;
                    }
                    break;
                case "RotateStructurePage":
                    switch (responseID)
                    {
                        case 1: // East
                            HandleRotateStructure(0.0f, true);
                            break;
                        case 2: // North
                            HandleRotateStructure(90.0f, true);
                            break;
                        case 3: // West
                            HandleRotateStructure(180.0f, true);
                            break;
                        case 4: // South
                            HandleRotateStructure(270.0f, true);
                            break;
                        case 5: // Rotate 20
                            HandleRotateStructure(20.0f, false);
                            break;
                        case 6: // Rotate 30
                            HandleRotateStructure(30.0f, false);
                            break;
                        case 7: // Rotate 45
                            HandleRotateStructure(45.0f, false);
                            break;
                        case 8: // Rotate 60
                            HandleRotateStructure(60.0f, false);
                            break;
                        case 9: // Rotate 75
                            HandleRotateStructure(75.0f, false);
                            break;
                        case 10: // Rotate 90
                            HandleRotateStructure(90.0f, false);
                            break;
                        case 11: // Rotate 180
                            HandleRotateStructure(180.0f, false);
                            break;
                        case 12: // Back
                            ChangePage("ManipulateStructurePage");
                            break;
                    }
                    break;
                case "RazeStructurePage":
                    switch (responseID)
                    {
                        case 1: // Raze Structure
                            HandleRazeStructure();
                            break;
                        case 2: // Back
                            ChangePage("ManipulateStructurePage");
                            break;
                    }
                    break;
            }
        }

        private void BuildMainMenuResponses(NWObject excludeObject)
        {
            NWPlayer oPC = GetPC();
            ClearPageResponses("MainPage");
            Model model = GetDialogCustomData<Model>();
            model.NearbyStructures.Clear();
            model.ActiveStructure = null;

            DialogResponse constructionSiteResponse = new DialogResponse(_color.Green("Create Construction Site"));
            if (_structure.CanPCBuildInLocation(GetPC(), model.TargetLocation, StructurePermission.CanBuildStructures) != 1)
            {
                constructionSiteResponse.IsActive = false;
            }

            AddResponseToPage("MainPage", constructionSiteResponse);
            int flagID = _structure.GetTerritoryFlagID(model.Flag);

            if (!_structure.PlayerHasPermission(oPC, StructurePermission.CanMoveStructures, flagID) &&
                    !_structure.PlayerHasPermission(oPC, StructurePermission.CanRazeStructures, flagID) &&
                    !_structure.PlayerHasPermission(oPC, StructurePermission.CanRotateStructures, flagID))
                return;

            for (int current = 1; current <= 30; current++)
            {
                NWPlaceable structure = NWPlaceable.Wrap(_.GetNearestObjectToLocation(NWScript.OBJECT_TYPE_PLACEABLE, model.TargetLocation, current));
                Location structureLocation = structure.Location;
                float distance = _.GetDistanceBetweenLocations(model.TargetLocation, structureLocation);

                if (distance > 15.0f) break;

                if (_structure.GetPlaceableStructureID(structure) > 0 && structure.GetLocalInt("IS_BUILDING_DOOR") == 0)
                {
                    model.NearbyStructures.Add(structure);
                }
            }

            foreach (NWPlaceable structure in model.NearbyStructures)
            {
                if (excludeObject == null || !Equals(excludeObject, structure))
                {
                    AddResponseToPage("MainPage", structure.Name, true, new Tuple<string, dynamic>(string.Empty, structure));
                }
            }
        }

        private void HandleMainMenuResponse(int responseID)
        {
            NWPlayer oPC = GetPC();
            Model model = GetDialogCustomData<Model>();
            DialogResponse response = GetResponseByID("MainPage", responseID);
            
            int flagID = _structure.GetTerritoryFlagID(model.Flag);

            if (responseID == 1)
            {
                _structure.CreateConstructionSite(GetPC(), model.TargetLocation);
                EndConversation();
                return;
            }

            NWPlaceable structure = (NWPlaceable)response.CustomData[string.Empty];
            if (structure != null)
            {
                int structureID = _structure.GetPlaceableStructureID(structure);
                model.ActiveStructure = structure;
                PCTerritoryFlagsStructure structureEntity = _structure.GetPCStructureByID(structureID);
                model.IsActiveStructureBuilding = structureEntity.StructureBlueprint.IsBuilding;

                SetResponseVisible("ManipulateStructurePage", 1, _structure.PlayerHasPermission(oPC, StructurePermission.CanRotateStructures, flagID));
                SetResponseVisible("ManipulateStructurePage", 2, _structure.PlayerHasPermission(oPC, StructurePermission.CanMoveStructures, flagID));
                SetResponseVisible("ManipulateStructurePage", 3, _structure.PlayerHasPermission(oPC, StructurePermission.CanRazeStructures, flagID));

                ChangePage("ManipulateStructurePage");
            }
        }

        private void HandleMoveStructure()
        {
            NWPlayer oPC = GetPC();
            Model model = GetDialogCustomData<Model>();
            int flagID = _structure.GetTerritoryFlagID(model.Flag);
            if (!_structure.PlayerHasPermission(oPC, StructurePermission.CanMoveStructures, flagID))
            {
                ChangePage("MainPage");
                return;
            }

            _.FloatingTextStringOnCreature("Please use your build tool to select a new location for this structure.", GetPC().Object, NWScript.FALSE);
            _structure.SetIsPCMovingStructure(GetPC(), model.ActiveStructure, true);
            EndConversation();
        }

        private void HandleRotateStructure(float rotation, bool isSet)
        {
            NWPlayer oPC = GetPC();
            Model model = GetDialogCustomData<Model>();
            int flagID = _structure.GetTerritoryFlagID(model.Flag);
            if (!_structure.PlayerHasPermission(oPC, StructurePermission.CanRotateStructures, flagID))
            {
                _.FloatingTextStringOnCreature("You do not have permission to rotate this structure.", oPC.Object, NWScript.FALSE);
                BuildMainMenuResponses(null);
                ChangePage("MainPage");
                return;
            }
            
            int structureID = _structure.GetPlaceableStructureID(model.ActiveStructure);
            PCTerritoryFlagsStructure entity = _structure.GetPCStructureByID(structureID);

            if (isSet)
            {
                entity.LocationOrientation = rotation;
            }
            else
            {
                double newOrientation = entity.LocationOrientation + rotation;
                while (newOrientation >= 360.0f)
                    newOrientation -= 360.0f;

                entity.LocationOrientation = newOrientation;
            }
            
            _structure.SaveChanges();

            NWPlaceable door = NWPlaceable.Wrap(model.ActiveStructure.GetLocalObject("BUILDING_ENTRANCE_DOOR"));
            bool hasDoor = door.IsValid;

            if (hasDoor)
            {
                door.Destroy();
            }

            NWPlaceable structure = model.ActiveStructure;
            structure.Facing = (float)entity.LocationOrientation;
            if (hasDoor)
            {
                NWPlaceable newDoor = _structure.CreateBuildingDoor(structure.Location, structureID);
                newDoor.SetLocalObject("BUILDING_DOOR_ENTRANCE", newDoor.Object);
            }
        }

        private void HandleRazeStructure()
        {
            NWPlayer oPC = GetPC();
            Model model = GetDialogCustomData<Model>();
            int flagID = _structure.GetTerritoryFlagID(model.Flag);

            if (!_structure.PlayerHasPermission(oPC, StructurePermission.CanRazeStructures, flagID))
            {
                _.FloatingTextStringOnCreature("You do not have permission to raze this structure.", oPC.Object, NWScript.FALSE);
                BuildMainMenuResponses(null);
                ChangePage("MainPage");
                return;
            }

            int structureID = _structure.GetPlaceableStructureID(model.ActiveStructure);

            if (model.IsConfirmingRaze)
            {
                model.IsConfirmingRaze = false;
                SetResponseText("RazeStructurePage", 1, _color.Red("Raze Structure"));
                PCTerritoryFlagsStructure entity = _structure.GetPCStructureByID(structureID);
                entity.IsActive = false;
                _structure.SaveChanges();

                _.DestroyObject(model.ActiveStructure.GetLocalObject("GateBlock"));
                _.DestroyObject(model.ActiveStructure.GetLocalObject("BUILDING_ENTRANCE_DOOR"));
                model.ActiveStructure.Destroy();
                
                BuildMainMenuResponses(model.ActiveStructure);
                ChangePage("MainPage");
            }
            else
            {
                model.IsConfirmingRaze = true;
                SetResponseText("RazeStructurePage", 1, _color.Red("CONFIRM RAZE STRUCTURE"));
            }
        }

        public override void EndDialog()
        {
        }
    }
}
