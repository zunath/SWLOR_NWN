using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class EditBuildingMode: ConversationBase
    {
        private class Model
        {
            public StructureModeType Mode { get; set; }
        }

        private readonly IColorTokenService _color;
        private readonly IBaseService _base;
        private readonly IDataService _data;

        public EditBuildingMode(
            INWScript script, 
            IDialogService dialog,
            IColorTokenService color,
            IBaseService @base,
            IDataService data) 
            : base(script, dialog)
        {
            _color = color;
            _base = @base;
            _data = data;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "<SET LATER>",
                "Set Mode: Residence",
                "Set Mode: Workshop",
                "Set Mode: Storefront");

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            var player = GetPC();
            var data = _base.GetPlayerTempData(player);
            var pcBaseStructureID = new Guid(data.TargetArea.GetLocalString("PC_BASE_STRUCTURE_ID"));
            var structure = _data.Get<PCBaseStructure>(pcBaseStructureID);

            string header = "You may change the active mode of this building here. Only one mode may be set at a time.\n\nBe aware that switching from a Residence mode will remove all primary residents for the building.\n\n";
            header += _color.Green("Current Mode: ");
            


        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}
