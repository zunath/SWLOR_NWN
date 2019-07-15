using System;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class EditBuildingMode: ConversationBase
    {
        private class Model
        {
            public StructureModeType Mode { get; set; }
        }
        
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "<SET LATER>",
                "Set Mode: Residence",
                "Set Mode: Workshop",
                "Set Mode: Storefront");

            DialogPage confirmSetMode = new DialogPage(
                "<SET LATER>",
                "Confirm Set Mode");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("ConfirmSetMode", confirmSetMode);
            return dialog;
        }

        public override void Initialize()
        {
            LoadMainPage();
        }
        
        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainResponses(responseID);
                    break;
                case "ConfirmSetMode":
                    ConfirmSetModeResponses(responseID);
                    break;
            }
        }

        private void LoadMainPage()
        {
            var player = GetPC();
            var data = BaseService.GetPlayerTempData(player);
            var pcBaseStructureID = new Guid(data.TargetArea.GetLocalString("PC_BASE_STRUCTURE_ID"));
            var structure = DataService.PCBaseStructure.GetByID(pcBaseStructureID);
            var mode = DataService.StructureMode.GetByID(structure.StructureModeID);
            var modeType = (StructureModeType) mode.ID;

            string header = "You may change the active mode of this building here. Only one mode may be set at a time.\n\nBe aware that switching modes will remove all primary residents for the building.\n\n";
            header += ColorTokenService.Green("Current Mode: ") + mode.Name;

            if (modeType == StructureModeType.Residence)
                SetResponseVisible("MainPage", 1, false);
            else if (modeType == StructureModeType.Workshop)
                SetResponseVisible("MainPage", 2, false);
            else if (modeType == StructureModeType.Storefront)
                SetResponseVisible("MainPage", 3, false);

            SetResponseVisible("MainPage", 3, false); // Temporarily hide the Storefront option until it's implemented.
            SetPageHeader("MainPage", header);
        }

        private void MainResponses(int responseID)
        {
            var model = GetDialogCustomData<Model>();
            switch (responseID)
            {
                case 1: // Set Mode: Residence
                    model.Mode = StructureModeType.Residence;
                    break;
                case 2: // Set Mode: Workshop
                    model.Mode = StructureModeType.Workshop;
                    break;
                case 3: // Set Mode: Storefront
                    model.Mode = StructureModeType.Storefront;
                    break;
            }

            LoadConfirmSetModePage();
            ChangePage("ConfirmSetMode");
        }

        private void LoadConfirmSetModePage()
        {
            string header;
            var model = GetDialogCustomData<Model>();

            switch (model.Mode)
            {
                case StructureModeType.None:
                    header = "No mode will be set. This is probably an error. Inform an admin.";
                    break;
                case StructureModeType.Residence:
                    header = "Setting this building to a Residence enables you or another player to set it as a primary residence. This grants an overall XP bonus to that player based on the number of furniture structures placed inside of the building. Persistent storage structures may also be placed inside of a residence. Crafting structures may NOT be used inside of residences.";
                    break;
                case StructureModeType.Workshop:
                    header = "Setting this building to a Workshop enables you to place crafting devices and workbenches down. All workbenches inside the building receive a bonus based on the number of furniture structures placed inside of the building. Persistent storage structures may also be placed inside of a workshop. Players may NOT set a workshop as a primary residence.";
                    break;
                case StructureModeType.Storefront:
                    header = "Setting this building to a Storefront enables you to hire NPCs to sell your wares. Persistent storage structures cannot store items but they will increase the number of items you may sell at a time. The daily salary of your merchants will reduce based on the number of furniture structures placed inside of the building. Crafting structures may NOT be used inside storefronts. Players may NOT set a storefront as a primary residence.";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            header += ColorTokenService.Red("\n\nWARNING:\n\n");
            header += GetWarning();

            SetPageHeader("ConfirmSetMode", header);
        }

        private string GetWarning()
        {
            var player = GetPC();
            var data = BaseService.GetPlayerTempData(player);
            var pcBaseStructureID = new Guid(data.TargetArea.GetLocalString("PC_BASE_STRUCTURE_ID"));
            var structure = DataService.PCBaseStructure.GetByID(pcBaseStructureID);
            var modeType = (StructureModeType)structure.StructureModeID;
            string warning;

            switch (modeType)
            {
                case StructureModeType.Residence:
                    warning = "- All primary residents will be removed and their XP bonuses will cease.\n";
                    break;
                case StructureModeType.Workshop:
                    warning = "- All crafting bonuses will cease.\n- All prices set on workbenches will be removed.\n";
                    break;
                case StructureModeType.Storefront:
                    warning = "- All hired NPCs will be fired and no refund for their salary will be given.\n- Items which were being sold will be sent to the planetary impound and you will need to pay to retrieve them.\n";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return warning;
        }

        private void ConfirmSetModeResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // Confirm
                    DoChangeMode();
                    break;
            }
        }

        private void DoChangeMode()
        {
            var model = GetDialogCustomData<Model>();
            var player = GetPC();
            var data = BaseService.GetPlayerTempData(player);
            var pcBaseStructureID = new Guid(data.TargetArea.GetLocalString("PC_BASE_STRUCTURE_ID"));
            var structure = DataService.PCBaseStructure.GetByID(pcBaseStructureID);
            var impoundedItems = 0;

            // Remove primary residents
            var primaryResident = DataService.Player.GetByPrimaryResidencePCBaseStructureIDOrDefault(pcBaseStructureID);
            if (primaryResident != null)
            {
                primaryResident.PrimaryResidencePCBaseStructureID = null;
                DataService.SubmitDataChange(primaryResident, DatabaseActionType.Update);
            }
            
            // Change mode
            structure.StructureModeID = (int)model.Mode;
            DataService.SubmitDataChange(structure, DatabaseActionType.Update);

            player.FloatingText("Building mode updated! " + impoundedItems + " item(s) were impounded.");
            EndConversation();
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}
