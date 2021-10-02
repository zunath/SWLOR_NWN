using System;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class TestWindowViewModel: GuiViewModelBase<TestWindowViewModel>
    {
        public GuiRectangle Geometry
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        public string ButtonText
        {
            get => Get<string>();
            set => Set(value);
        }

        public int SelectedComboBoxValue
        {
            get => Get<int>();
            set => Set(value);
        }

        public bool IsToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiColor SelectedColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public bool IsChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string SelectedOption
        {
            get => Get<string>();
            set => Set(value);
        }

        public string EnteredText
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Placeholder
        {
            get => Get<string>();
            set => Set(value);
        }

        public TestWindowViewModel()
        {
            Geometry = new GuiRectangle(20, 40, 200, 600);
        }

        public Action OnWindowOpen() => () =>
        {
            NuiSetBindWatch(Player, WindowToken, nameof(Geometry), true);
            NuiSetBind(Player, WindowToken, nameof(Geometry), Geometry.ToJson());
        };

        public Action OnWindowClosed() => () =>
        {
        };

        public Action OnClickedFirstButton() => () =>
        {

            EnteredText += "A";
        };

    }
}
