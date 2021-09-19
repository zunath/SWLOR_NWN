using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.DataModel
{
    public class TestWindowGuiDataModel: IGuiDataModel
    {
        public int ButtonText { get; set; }
        public int SelectedComboBoxValue { get; set; }
    }
}
