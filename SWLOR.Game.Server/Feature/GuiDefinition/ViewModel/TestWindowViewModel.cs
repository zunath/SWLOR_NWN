using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class TestWindowViewModel: GuiViewModelBase
    {
        public string ButtonText { get; set; } = "My Button";
        public int SelectedComboBoxValue { get; set; }
        public bool IsToggled { get; set; }
        public GuiColor SelectedColor { get; set; }
        public bool IsChecked { get; set; }
        public string SelectedOption { get; set; }
        public string EnteredText { get; set; }

    }

    

}
