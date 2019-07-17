using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

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
        
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "Which blueprints would you like to view?",
                "Crafting Blueprints"
            );

            DialogPage craftCategoriesPage = new DialogPage(
                "Which category would you like to view?"
            );

            DialogPage blueprintList = new DialogPage(
                "Which blueprint would you like to view?"
            );

            DialogPage blueprintDetails = new DialogPage(
                "<SET LATER>"
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

            vm.CraftCategories = CraftService.GetCategoriesAvailableToPC(GetPC().GlobalID);

            foreach (CraftBlueprintCategory category in vm.CraftCategories)
            {
                AddResponseToPage("CraftCategoriesPage", category.Name, true, category.ID);
            }

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

            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        private void HandleMainPageResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Crafting Blueprints
                    ChangePage("CraftCategoriesPage");
                    break;
            }
        }

        private void HandleCraftCategoriesPageResponse(int responseID)
        {
            Model vm = GetDialogCustomData<Model>();
            ClearPageResponses("BlueprintListPage");
            DialogResponse response = GetResponseByID("CraftCategoriesPage", responseID);
            int categoryID = (int) response.CustomData;
            
            vm.CraftBlueprints = CraftService.GetPCBlueprintsByCategoryID(GetPC().GlobalID, categoryID);

            foreach (CraftBlueprint bp in vm.CraftBlueprints)
            {
                AddResponseToPage("BlueprintListPage", bp.ItemName, true, bp.ID);
            }

            ChangePage("BlueprintListPage");
        }

        private void HandleBlueprintListPageResponse(int responseID)
        {
            DialogResponse response = GetResponseByID("BlueprintListPage", responseID);
            int blueprintID = (int)response.CustomData;

            if (blueprintID == -1)
            {
                ChangePage("CraftCategoriesPage");
                return;
            }

            var model = CraftService.GetPlayerCraftingData(GetPC());
            model.Blueprint = CraftService.GetBlueprintByID(blueprintID);
            model.BlueprintID = blueprintID;
            model.PlayerSkillRank = SkillService.GetPCSkillRank(GetPC(), model.Blueprint.SkillID);
            model.MainMinimum = model.Blueprint.MainMinimum;
            model.MainMaximum = model.Blueprint.MainMaximum;
            model.SecondaryMinimum = model.Blueprint.SecondaryMinimum;
            model.SecondaryMaximum = model.Blueprint.SecondaryMaximum;
            model.TertiaryMinimum = model.Blueprint.TertiaryMinimum;
            model.TertiaryMaximum = model.Blueprint.TertiaryMaximum;

            string header = CraftService.BuildBlueprintHeader(GetPC(), false);

            SetPageHeader("BlueprintDetailsPage", header);
            ChangePage("BlueprintDetailsPage");
        }
        
        public override void EndDialog()
        {
            CraftService.ClearPlayerCraftingData(GetPC());
        }
    }
}
