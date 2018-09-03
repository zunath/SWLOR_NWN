using System;
using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class ViewBlueprints : ConversationBase
    {
        private class Model
        {
            public List<CraftBlueprint> CraftBlueprints { get; set; }
            public List<CraftBlueprintCategory> CraftCategories { get; set; }
        }

        private readonly ICraftService _craft;

        public ViewBlueprints(
            INWScript script,
            IDialogService dialog,
            ICraftService craft)
            : base(script, dialog)
        {
            _craft = craft;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "Which blueprints would you like to view?",
                "Crafting Blueprints",
                "Back"
            );

            DialogPage craftCategoriesPage = new DialogPage(
                "Which category would you like to view?"
            );

            DialogPage blueprintList = new DialogPage(
                "Which blueprint would you like to view?"
            );

            DialogPage blueprintDetails = new DialogPage(
                "<SET LATER>",
                "Back"
            );

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("CraftCategoriesPage", craftCategoriesPage);
            dialog.AddPage("BlueprintListPage", blueprintList);
            dialog.AddPage("BlueprintDetailsPage", blueprintDetails);

            return dialog;
        }

        public override void Initialize()
        {
            Model vm = GetDialogCustomData<Model>();

            vm.CraftCategories = _craft.GetCategoriesAvailableToPC(GetPC().GlobalID);

            foreach (CraftBlueprintCategory category in vm.CraftCategories)
            {
                AddResponseToPage("CraftCategoriesPage", category.Name, true, new Tuple<string, dynamic>(string.Empty, category.CraftBlueprintCategoryID));
            }
            AddResponseToPage("CraftCategoriesPage", "Back", true, new Tuple<string, dynamic>(string.Empty, -1));

            SetDialogCustomData(vm);
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    HandleMainPageResponse(responseID);
                    break;
                case "CraftCategoriesPage":
                    HandleCraftCategoriesPageResponse(responseID);
                    break;
                case "BlueprintListPage":
                    HandleBlueprintListPageResponse(responseID);
                    break;
                case "BlueprintDetailsPage":
                    HandleBlueprintDetailsPageResponse(responseID);
                    break;

            }
        }

        private void HandleMainPageResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Crafting Blueprints
                    ChangePage("CraftCategoriesPage");
                    break;
                case 2: // Back
                    SwitchConversation("RestMenu");
                    break;
            }
        }

        private void HandleCraftCategoriesPageResponse(int responseID)
        {
            Model vm = GetDialogCustomData<Model>();
            ClearPageResponses("BlueprintListPage");
            DialogResponse response = GetResponseByID("CraftCategoriesPage", responseID);
            int categoryID = (int)response.CustomData[string.Empty];

            if (categoryID == -1) // Back
            {
                ChangePage("MainPage");
                return;
            }

            vm.CraftBlueprints = _craft.GetPCBlueprintsByCategoryID(GetPC().GlobalID, categoryID);

            foreach (CraftBlueprint bp in vm.CraftBlueprints)
            {
                AddResponseToPage("BlueprintListPage", bp.ItemName, true, new Tuple<string, dynamic>(string.Empty, bp.CraftBlueprintID));
            }
            AddResponseToPage("BlueprintListPage", "Back", true, new Tuple<string, dynamic>(string.Empty, -1));

            ChangePage("BlueprintListPage");
        }

        private void HandleBlueprintListPageResponse(int responseID)
        {
            Model vm = GetDialogCustomData<Model>();
            DialogResponse response = GetResponseByID("BlueprintListPage", responseID);
            int blueprintID = (int)response.CustomData[string.Empty];

            if (blueprintID == -1)
            {
                ChangePage("CraftCategoriesPage");
                return;
            }

            string header = _craft.BuildBlueprintHeader(GetPC(), blueprintID);

            SetPageHeader("BlueprintDetailsPage", header);
            ChangePage("BlueprintDetailsPage");
        }

        private void HandleBlueprintDetailsPageResponse(int responseID)
        {
            if (responseID == 1)
            {
                ChangePage("BlueprintListPage");
            }
        }
        public override void EndDialog()
        {
        }
    }
}
