using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using SWLOR.Game.Server.ValueObject.Structure;

namespace SWLOR.Game.Server.Conversation
{
    public class ConstructionSite: ConversationBase
    {
        private class Model
        {
            public int ConstructionSiteID { get; set; }
            public bool IsPreviewing { get; set; }
            public int CategoryID { get; set; }
            public int BlueprintID { get; set; }
            public bool IsTerritoryFlag { get; set; }
            public string RecoverResourcesHeader { get; set; }
            public int FlagID { get; set; }
        }

        private readonly IColorTokenService _color;
        private readonly IStructureService _structure;
        private readonly IAuthorizationService _authorization;
        private readonly IItemService _item;
        private readonly ISkillService _skill;

        public ConstructionSite(
            INWScript script, 
            IDialogService dialog,
            IColorTokenService color,
            IStructureService structure,
            IAuthorizationService authorization,
            IItemService item,
            ISkillService skill) 
            : base(script, dialog)
        {
            _color = color;
            _structure = structure;
            _authorization = authorization;
            _item = item;
            _skill = skill;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage("<SET LATER>");
            DialogPage blueprintCategoryPage = new DialogPage(
                    "Please select a blueprint category. New blueprints will unlock after purchasing the prerequisite Perk. Perks can be purchased in your rest menu (press R)."
            );

            DialogPage blueprintDetailsPage = new DialogPage(
                    "<SET LATER>",
                    "Select Blueprint",
                    "Preview",
                    "Back"
            );

            DialogPage razePage = new DialogPage(
                    _color.Red("WARNING: ") + "Razing this construction site will destroy it permanently. Materials used will NOT be returned to you.\n\n" +
                            "Are you sure you want to raze this construction site?",
                    _color.Red("Confirm Raze"),
                    "Back"
            );

            DialogPage quickBuildPage = new DialogPage(
                    "Quick building this structure will complete it instantly. Please use this sparingly.",
                    "Confirm Quick Build",
                    "Back"
            );

            DialogPage blueprintListPage = new DialogPage(
                    "Please select a blueprint."
            );

            DialogPage rotatePage = new DialogPage(
                    "Please select a rotation.",
                    "Set Facing: East",
                    "Set Facing: North",
                    "Set Facing: West",
                    "Set Facing: South",
                    "Rotate 20°",
                    "Rotate 30°",
                    "Rotate 45°",
                    "Rotate 60°",
                    "Rotate 75°",
                    "Rotate 90°",
                    "Rotate 180°",
                    "Back"
            );

            DialogPage recoverResourcesPage = new DialogPage(
                    "This construction site is in an invalid state or location.\n\n" +
                    "Possible reasons:\n" +
                    "1.) The territory cannot manage any more structures.\n" +
                    "2.) The area of influence of this construction site's territory flag blueprint overlaps with another territory's.\n\n" +
                    "You may raze this construction site to recover all spent resources.",
                    "Raze & Recover Resources"
            );

            DialogPage changeLayoutPage = new DialogPage(
                    "Please select a new interior layout for your building."
            );

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("BlueprintCategoryPage", blueprintCategoryPage);
            dialog.AddPage("BlueprintListPage", blueprintListPage);
            dialog.AddPage("BlueprintDetailsPage", blueprintDetailsPage);
            dialog.AddPage("QuickBuildPage", quickBuildPage);
            dialog.AddPage("RotatePage", rotatePage);
            dialog.AddPage("RazePage", razePage);
            dialog.AddPage("RecoverResourcesPage", recoverResourcesPage);
            dialog.AddPage("ChangeLayoutPage", changeLayoutPage);
            return dialog;
        }

        public override void Initialize()
        {
            InitializeConstructionSite();

            if (!_structure.IsConstructionSiteValid((NWPlaceable)GetDialogTarget()))
            {
                ChangePage("RecoverResourcesPage");
            }
            else
            {
                BuildMainPage();
            }
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    HandleMainPageResponse(responseID);
                    break;
                case "BlueprintCategoryPage":
                    HandleCategoryResponse(responseID);
                    break;
                case "BlueprintListPage":
                    HandleBlueprintListResponse(responseID);
                    break;
                case "BlueprintDetailsPage":
                    HandleBlueprintDetailsResponse(responseID);
                    break;
                case "RazePage":
                    HandleRazeResponse(responseID);
                    break;
                case "QuickBuildPage":
                    HandleQuickBuildResponse(responseID);
                    break;
                case "RotatePage":
                    HandleRotatePageResponse(responseID);
                    break;
                case "RecoverResourcesPage":
                    HandleRecoverResourcesPage(responseID);
                    break;
                case "ChangeLayoutPage":
                    HandleChangeLayoutPageResponse(responseID);
                    break;
            }
        }

        public override void EndDialog()
        {
        }

        private void InitializeConstructionSite()
        {
            NWPlaceable site = (NWPlaceable)GetDialogTarget();
            NWObject existingFlag = _structure.GetTerritoryFlagOwnerOfLocation(GetDialogTarget().Location);
            Model model = GetDialogCustomData<Model>();

            float distance = _.GetDistanceBetween(existingFlag.Object, GetDialogTarget().Object);
            if (!existingFlag.IsValid)
            {
                model.IsTerritoryFlag = true;
            }
            else
            {
                int flagID = _structure.GetTerritoryFlagID(existingFlag);
                model.FlagID = flagID;
                PCTerritoryFlag entity = _structure.GetPCTerritoryFlagByID(flagID);
                if (distance <= entity.StructureBlueprint.MaxBuildDistance || Equals(site.Area, existingFlag))
                {
                    model.IsTerritoryFlag = false;
                }
                else
                {
                    model.IsTerritoryFlag = true;
                }
            }

            model.ConstructionSiteID = _structure.GetConstructionSiteID(site);
            SetDialogCustomData(model);
        }


        private void BuildMainPage()
        {
            NWPlayer oPC = GetPC();
            string header;
            Model model = GetDialogCustomData<Model>();

            ClearPageResponses("MainPage");

            if (model.ConstructionSiteID <= 0)
            {
                header = "Please select an option.";

                if (model.IsTerritoryFlag)
                {
                    AddResponseToPage("MainPage", "Select Blueprint");
                    AddResponseToPage("MainPage", "Move");
                    AddResponseToPage("MainPage", _color.Red("Raze"));
                }
                else
                {

                    AddResponseToPage("MainPage", "Select Blueprint", _structure.PlayerHasPermission(oPC, StructurePermission.CanBuildStructures, model.FlagID));
                    AddResponseToPage("MainPage", "Move", _structure.PlayerHasPermission(oPC, StructurePermission.CanMoveStructures, model.FlagID));
                    AddResponseToPage("MainPage", _color.Red("Raze"), _structure.PlayerHasPermission(oPC, StructurePermission.CanRazeStructures, model.FlagID));
                }

            }
            else
            {
                Data.Entities.ConstructionSite entity = _structure.GetConstructionSiteByID(model.ConstructionSiteID);

                header = _color.Green("Blueprint: ") + entity.StructureBlueprint.Name + "\n";
                if (entity.StructureBlueprint.IsVanity)
                {
                    header += _color.Green("Type: ") + "Vanity\n";
                }
                if (entity.StructureBlueprint.IsSpecial)
                {
                    header += _color.Green("Type: ") + "Special\n";
                }
                if (entity.StructureBlueprint.IsResource)
                {
                    header += _color.Green("Type: ") + "Resource\n";
                }
                if (entity.StructureBlueprint.IsBuilding)
                {
                    header += _color.Green("Type: ") + "Building\n";
                }

                header += _color.Green("Level: ") + entity.StructureBlueprint.Level + "\n";

                header += _color.Green("Required Tool Level: ") + entity.StructureBlueprint.CraftTierLevel + "\n\n";


                if (entity.StructureBlueprint.MaxBuildDistance > 0.0f)
                {
                    header += _color.Green("Build Distance: ") + entity.StructureBlueprint.MaxBuildDistance + " meters" + "\n";
                }
                if (entity.StructureBlueprint.VanityCount > 0)
                {
                    header += _color.Green("Max # of Vanity Structures: ") + entity.StructureBlueprint.VanityCount + "\n";
                }
                if (entity.StructureBlueprint.SpecialCount > 0)
                {
                    header += _color.Green("Max # of Special Structures: ") + entity.StructureBlueprint.SpecialCount + "\n";
                }
                if (entity.StructureBlueprint.ItemStorageCount > 0)
                {
                    header += _color.Green("Item Storage: ") + entity.StructureBlueprint.ItemStorageCount + " items" + "\n";
                }
                if (entity.BuildingInterior != null)
                {
                    header += _color.Green("Interior Layout: ") + entity.BuildingInterior.Name + "\n";
                }

                header += _color.Green("Resources Required: ") + "\n\n";

                foreach (ConstructionSiteComponent comp in entity.ConstructionSiteComponents)
                {
                    header += comp.Quantity > 0 ? comp.Quantity + "x " + _item.GetNameByResref(comp.StructureComponent.Resref) + "\n" : "";
                }

                AddResponseToPage("MainPage", "Quick Build", _authorization.IsPCRegisteredAsDM(GetPC()));
                AddResponseToPage("MainPage", "Build");
                AddResponseToPage("MainPage", "Preview");
                AddResponseToPage("MainPage", "Preview Interior", entity.BuildingInterior != null);
                AddResponseToPage("MainPage", "Change Interior Layout", entity.BuildingInterior != null);
                AddResponseToPage("MainPage", "Rotate", _structure.PlayerHasPermission(oPC, StructurePermission.CanRotateStructures, model.FlagID));
                AddResponseToPage("MainPage", "Move", _structure.PlayerHasPermission(oPC, StructurePermission.CanMoveStructures, model.FlagID));
                AddResponseToPage("MainPage", _color.Red("Raze"), _structure.PlayerHasPermission(oPC, StructurePermission.CanRazeStructures, model.FlagID));
            }

            SetPageHeader("MainPage", header);
        }

        private void ToggleRotateOptions()
        {
            Model model = GetDialogCustomData<Model>();
            Data.Entities.ConstructionSite site = _structure.GetConstructionSiteByID(model.ConstructionSiteID);

            bool isVisible = !(site != null && site.StructureBlueprint.IsBuilding);

            SetResponseVisible("RotatePage", 5, isVisible);
            SetResponseVisible("RotatePage", 6, isVisible);
            SetResponseVisible("RotatePage", 7, isVisible);
            SetResponseVisible("RotatePage", 8, isVisible);
            SetResponseVisible("RotatePage", 9, isVisible);
            SetResponseVisible("RotatePage", 10, isVisible);
            SetResponseVisible("RotatePage", 11, isVisible);
        }

        private void HandleMainPageResponse(int responseID)
        {
            Model model = GetDialogCustomData<Model>();

            if (model.ConstructionSiteID <= 0)
            {
                switch (responseID)
                {
                    case 1: // Select Blueprint

                        LoadCategoryPageResponses();
                        ChangePage("BlueprintCategoryPage");
                        break;
                    case 2: // Move
                        _structure.SetIsPCMovingStructure(GetPC(), (NWPlaceable)GetDialogTarget(), true);
                        _.FloatingTextStringOnCreature("Please use your build tool to select a new location for this structure.", GetPC().Object, NWScript.FALSE);
                        EndConversation();
                        break;
                    case 3: // Raze
                        ChangePage("RazePage");
                        break;
                }
            }
            else
            {
                switch (responseID)
                {
                    case 1: // Quick Build
                        ChangePage("QuickBuildPage");
                        break;
                    case 2: // Build
                        NWObject target = GetDialogTarget();
                        GetPC().AssignCommand(() => _.ActionAttack(target.Object));
                        break;
                    case 3: // Preview
                        DoConstructionSitePreview();
                        break;
                    case 4: // Preview Interior
                        DoBuildingInteriorPreview();
                        break;
                    case 5: // Change Interior Layout
                        LoadChangeLayoutResponses();
                        ChangePage("ChangeLayoutPage");
                        break;
                    case 6: // Rotate
                        ToggleRotateOptions();
                        ChangePage("RotatePage");
                        break;
                    case 7: // Move
                        _structure.SetIsPCMovingStructure(GetPC(), (NWPlaceable)GetDialogTarget(), true);
                        _.FloatingTextStringOnCreature("Please use your build tool to select a new location for this structure.", GetPC().Object, NWScript.FALSE);
                        EndConversation();
                        break;
                    case 8: // Raze
                        ChangePage("RazePage");
                        break;
                }
            }

        }

        private void BuildBlueprintDetailsHeader()
        {
            Model model = GetDialogCustomData<Model>();
            string header = _structure.BuildMenuHeader(model.BlueprintID);
            SetPageHeader("BlueprintDetailsPage", header);
        }

        private void LoadCategoryPageResponses()
        {
            Model model = GetDialogCustomData<Model>();
            ClearPageResponses("BlueprintCategoryPage");

            PCTerritoryFlag flag = _structure.GetPCTerritoryFlagByID(model.FlagID);
            TerritoryStructureCount counts = _structure.GetNumberOfStructuresInTerritory(model.FlagID);

            List<StructureCategory> categories = _structure.GetStructureCategoriesByType(GetPC().GlobalID, model.IsTerritoryFlag, false, false, false, false).ToList();
            if (flag != null && counts.VanityCount < flag.StructureBlueprint.VanityCount)
            {
                categories.AddRange(_structure.GetStructureCategoriesByType(GetPC().GlobalID, model.IsTerritoryFlag, true, false, false, false));
            }
            if (flag != null && counts.SpecialCount < flag.StructureBlueprint.SpecialCount)
            {
                categories.AddRange(_structure.GetStructureCategoriesByType(GetPC().GlobalID, model.IsTerritoryFlag, false, true, false, false));
            }
            if (flag != null && counts.ResourceCount < flag.StructureBlueprint.ResourceCount)
            {
                categories.AddRange(_structure.GetStructureCategoriesByType(GetPC().GlobalID, model.IsTerritoryFlag, false, false, true, false));
            }
            if (flag != null && counts.BuildingCount < flag.StructureBlueprint.BuildingCount)
            {
                categories.AddRange(_structure.GetStructureCategoriesByType(GetPC().GlobalID, model.IsTerritoryFlag, false, false, false, true));
            }

            foreach (StructureCategory category in categories)
            {
                AddResponseToPage("BlueprintCategoryPage", category.Name, category.IsActive, new Tuple<string, dynamic>(string.Empty, category.StructureCategoryID));
            }
            
            AddResponseToPage("BlueprintCategoryPage", "Back");
        }

        private void LoadBlueprintListPageResponses()
        {
            Model model = GetDialogCustomData<Model>();

            ClearPageResponses("BlueprintListPage");
            Location location = GetDialogTarget().Location;
            PCSkill pcSkill = _skill.GetPCSkill(GetPC(), SkillType.Construction);
            if (pcSkill == null) return;
            
            PCTerritoryFlag flag = _structure.GetPCTerritoryFlagByID(model.FlagID);
            TerritoryStructureCount counts = _structure.GetNumberOfStructuresInTerritory(model.FlagID);

            List<StructureBlueprint> blueprints = _structure.GetStructuresByCategoryAndType(GetPC().GlobalID, model.CategoryID, false, false, false, false); // Territory markers
            if (flag != null && counts.VanityCount < flag.StructureBlueprint.VanityCount)
            {
                blueprints.AddRange(_structure.GetStructuresByCategoryAndType(GetPC().GlobalID, model.CategoryID, true, false, false, false)); // Vanity
            }
            if (flag != null && counts.SpecialCount < flag.StructureBlueprint.SpecialCount)
            {
                blueprints.AddRange(_structure.GetStructuresByCategoryAndType(GetPC().GlobalID, model.CategoryID, false, true, false, false)); // Special
            }
            if (flag != null && counts.ResourceCount < flag.StructureBlueprint.ResourceCount)
            {
                blueprints.AddRange(_structure.GetStructuresByCategoryAndType(GetPC().GlobalID, model.CategoryID, false, false, true, false)); // Resource
            }
            if (flag != null && counts.BuildingCount < flag.StructureBlueprint.BuildingCount)
            {
                blueprints.AddRange(_structure.GetStructuresByCategoryAndType(GetPC().GlobalID, model.CategoryID, false, false, false, true)); // Building
            }

            foreach (StructureBlueprint entity in blueprints)
            {
                string entityName = entity.Name + " (Lvl. " + entity.Level + ")";
                if (model.IsTerritoryFlag)
                {
                    if (_structure.WillBlueprintOverlapWithExistingFlags(location, entity.StructureBlueprintID))
                    {
                        entityName = _color.Red(entityName + " [OVERLAPS]");
                    }
                }
                AddResponseToPage("BlueprintListPage", entityName, entity.IsActive, new Tuple<string, dynamic>(string.Empty, entity.StructureBlueprintID));
            }
            
            AddResponseToPage("BlueprintListPage", "Back");
        }

        private void HandleCategoryResponse(int responseID)
        {
            DialogResponse response = GetResponseByID("BlueprintCategoryPage", responseID);
            if (!response.HasCustomData)
            {
                ChangePage("MainPage");
                return;
            }

            Model model = GetDialogCustomData<Model>();
            model.CategoryID = (int) response.CustomData[string.Empty];
            LoadBlueprintListPageResponses();
            ChangePage("BlueprintListPage");
        }

        private void HandleBlueprintListResponse(int responseID)
        {
            DialogResponse response = GetResponseByID("BlueprintListPage", responseID);
            if (!response.HasCustomData)
            {
                ChangePage("BlueprintCategoryPage");
                return;
            }

            Model model = GetDialogCustomData<Model>();
            model.BlueprintID = (int) response.CustomData[string.Empty];
            BuildBlueprintDetailsHeader();
            ChangePage("BlueprintDetailsPage");
        }

        private void HandleBlueprintDetailsResponse(int responseID)
        {
            Model model = GetDialogCustomData<Model>();

            switch (responseID)
            {
                case 1: // Select Blueprint
                    DoSelectBlueprint();
                    break;
                case 2: // Preview
                    DoBlueprintPreview(model.BlueprintID);
                    break;
                case 3: // Back
                    ChangePage("BlueprintListPage");
                    break;
            }
        }

        private void HandleRotatePageResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // East
                    DoRotateConstructionSite(0.0f, true);
                    break;
                case 2: // North
                    DoRotateConstructionSite(90.0f, true);
                    break;
                case 3: // West
                    DoRotateConstructionSite(180.0f, true);
                    break;
                case 4: // South
                    DoRotateConstructionSite(270.0f, true);
                    break;
                case 5: // Rotate 20
                    DoRotateConstructionSite(20.0f, false);
                    break;
                case 6: // Rotate 30
                    DoRotateConstructionSite(30.0f, false);
                    break;
                case 7: // Rotate 45
                    DoRotateConstructionSite(45.0f, false);
                    break;
                case 8: // Rotate 60
                    DoRotateConstructionSite(60.0f, false);
                    break;
                case 9: // Rotate 75
                    DoRotateConstructionSite(75.0f, false);
                    break;
                case 10: // Rotate 90
                    DoRotateConstructionSite(90.0f, false);
                    break;
                case 11: // Rotate 180
                    DoRotateConstructionSite(180.0f, false);
                    break;
                case 12: // Back
                    ChangePage("MainPage");
                    break;
            }
        }

        private void HandleRazeResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Confirm Raze
                    DoRaze();
                    break;
                case 2: // Back
                    ChangePage("MainPage");
                    break;
            }
        }

        private void HandleQuickBuildResponse(int responseID)
        {
            switch (responseID)
            {
                case 1:
                    DoQuickBuild();
                    break;
                case 2: // Back
                    ChangePage("MainPage");
                    break;
            }
        }

        private void DoBlueprintPreview(int blueprintID)
        {
            Model model = GetDialogCustomData<Model>();
            if (model.IsPreviewing) return;
            model.IsPreviewing = true;

            StructureBlueprint entity = _structure.GetStructureBlueprintByID(blueprintID);
            NWPlaceable preview = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, entity.Resref, GetDialogTarget().Location));
            preview.IsUseable = false;
            preview.IsPlot = true;
            preview.Destroy(6.0f);
            preview.DelayCommand(() =>
            {
                model.IsPreviewing = false;
            }, 6.0f);
        }

        private void DoConstructionSitePreview()
        {
            Model model = GetDialogCustomData<Model>();
            if (model.IsPreviewing) return;
            
            Data.Entities.ConstructionSite entity = _structure.GetConstructionSiteByID(model.ConstructionSiteID);
            StructureBlueprint blueprint = entity.StructureBlueprint;
            NWPlaceable preview = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, blueprint.Resref, GetDialogTarget().Location));
            preview.IsUseable = false;
            preview.IsPlot = true;
            preview.Destroy(6.0f);
            preview.DelayCommand(() =>
            {
                model.IsPreviewing = false;
            }, 6.0f);
        }

        private void DoRaze()
        {
            Model model = GetDialogCustomData<Model>();
            NWPlayer oPC = GetPC();

            if (!_structure.PlayerHasPermission(oPC, StructurePermission.CanRazeStructures, model.FlagID))
            {
                _.FloatingTextStringOnCreature("You do not have permission to raze structures.", oPC.Object, NWScript.FALSE);
                BuildMainPage();
                ChangePage("MainPage");
                return;
            }

            _structure.RazeConstructionSite(GetPC(), (NWPlaceable)GetDialogTarget(), false);
            EndConversation();
        }

        private void DoQuickBuild()
        {
            NWPlaceable completedStructure = _structure.CompleteStructure((NWPlaceable)GetDialogTarget());
            _structure.LogQuickBuildAction(GetPC(), completedStructure);
            EndConversation();
        }

        private void DoSelectBlueprint()
        {
            NWPlayer oPC = GetPC();
            Model model = GetDialogCustomData<Model>();

            if (!_structure.PlayerHasPermission(oPC, StructurePermission.CanBuildStructures, model.FlagID))
            {
                oPC.FloatingText("You do not have permission to build structures.");
                BuildMainPage();
                ChangePage("MainPage");
                return;
            }

            if (model.IsTerritoryFlag &&
                    _structure.WillBlueprintOverlapWithExistingFlags(GetDialogTarget().Location, model.BlueprintID))
            {
                GetPC().FloatingText("Unable to select blueprint. Area of influence would overlap with another territory.");
            }
            else
            {
                _structure.SelectBlueprint(GetPC(), (NWPlaceable)GetDialogTarget(), model.BlueprintID);
                EndConversation();
            }
        }


        private void DoRotateConstructionSite(float rotation, bool isSet)
        {
            NWPlayer oPC = GetPC();
            Model model = GetDialogCustomData<Model>();

            if (!_structure.PlayerHasPermission(oPC, StructurePermission.CanRotateStructures, model.FlagID))
            {
                oPC.FloatingText("You do not have permission to rotate structures.");
                BuildMainPage();
                ChangePage("MainPage");
                return;
            }
            
            int constructionSiteID = _structure.GetConstructionSiteID((NWPlaceable)GetDialogTarget());
            Data.Entities.ConstructionSite entity = _structure.GetConstructionSiteByID(constructionSiteID);

            if (isSet)
            {
                entity.LocationOrientation = rotation;
            }
            else
            {
                entity.LocationOrientation = entity.LocationOrientation + rotation;
            }

            _structure.SaveChanges();
            GetDialogTarget().Facing = (float) entity.LocationOrientation;
        }

        private void HandleRecoverResourcesPage(int responseID)
        {
            switch (responseID)
            {
                case 1: // Raze & Recover Resources
                    _structure.RazeConstructionSite(GetPC(), (NWPlaceable)GetDialogTarget(), true);
                    EndConversation();
                    break;
            }
        }

        private void LoadChangeLayoutResponses()
        {
            ClearPageResponses("ChangeLayoutPage");

            Model model = GetDialogCustomData<Model>();

            StructureBlueprint blueprint = _structure.GetConstructionSiteByID(model.ConstructionSiteID).StructureBlueprint;
            if (!blueprint.IsBuilding || blueprint.BuildingCategory == null) return;

            List<BuildingInterior> options = _structure.GetBuildingInteriorsByCategoryID(blueprint.BuildingCategory.BuildingCategoryID);

            foreach (BuildingInterior option in options)
            {
                AddResponseToPage("ChangeLayoutPage", option.Name, true, new Tuple<string, dynamic>(string.Empty, option));
            }

            AddResponseToPage("ChangeLayoutPage", "Back");
        }

        private void HandleChangeLayoutPageResponse(int responseID)
        {
            DialogResponse response = GetResponseByID("ChangeLayoutPage", responseID);

            if (!response.HasCustomData)
            {
                ChangePage("MainPage");
                return;
            }

            BuildingInterior interior = (BuildingInterior)response.CustomData[string.Empty];
            Model model = GetDialogCustomData<Model>();

            Data.Entities.ConstructionSite site = _structure.GetConstructionSiteByID(model.ConstructionSiteID);
            site.BuildingInteriorID = interior.BuildingInteriorID;
            _structure.SaveChanges();

            BuildMainPage();
            ChangePage("MainPage");
        }

        private void DoBuildingInteriorPreview()
        {
            Model model = GetDialogCustomData<Model>();
            Data.Entities.ConstructionSite site = _structure.GetConstructionSiteByID(model.ConstructionSiteID);

            if (!site.StructureBlueprint.IsBuilding || site.BuildingInterior == null) return;

            _structure.PreviewBuildingInterior(GetPC(), site.BuildingInterior.BuildingInteriorID);
        }

    }
}
