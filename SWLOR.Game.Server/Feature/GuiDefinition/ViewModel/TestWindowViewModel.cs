using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class TestWindowViewModel: GuiViewModelBase
    {
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

        public void Test()
        {

        }

        public Action OnWindowOpen() => () =>
        {
            Console.WriteLine("hello from window open");
        };

        public Action OnWindowClosed() => () =>
        {
            Console.WriteLine("hello from window closed");
        };

        public Action OnClickedFirstButton() => () =>
        {
            Console.WriteLine("hello from button click");

            EnteredText += "A";
        };

    }
}
