using System;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PriceSelectionViewModel: GuiViewModelBase<PriceSelectionViewModel>
    {
        private string _targetRecordId;
        private GuiWindowType _targetWindowType;

        public string ItemName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Price
        {
            get => Get<string>();
            set
            {
                var newPrice = Regex.Replace(value, "[^0-9]", string.Empty);
                newPrice = newPrice.TrimStart('0');

                // If nothing is entered, default to zero.
                if (newPrice.Length < 1)
                    newPrice = "0";

                // Ensure we can convert the number. If we can't, reduce it to zero.
                if (!int.TryParse(newPrice, out var result))
                {
                    newPrice = "0";
                }

                // Handle negative prices.
                if (result < 0)
                    newPrice = "0";

                // Handle max
                if (result > 9999999)
                    newPrice = "9999999";

                Set(newPrice);
            }
        }

        public void SpecifyTargetWindow(GuiWindowType windowType, string recordId, int currentPrice, string itemName)
        {
            _targetRecordId = recordId;
            _targetWindowType = windowType;
            Price = currentPrice.ToString();
            ItemName = $"Price For: {itemName}";
        }

        public Action OnLoadWindow() => () =>
        {
            WatchOnClient(model => model.Price);
        };

        public Action OnClickSave() => () =>
        {
            var window = Gui.GetPlayerWindow(Player, _targetWindowType);
            var vm = (IGuiAcceptsPriceChange)window.ViewModel;
            vm.ChangePrice(_targetRecordId, Convert.ToInt32(Price));

            Gui.TogglePlayerWindow(Player, GuiWindowType.PriceSelection);
        };

        public Action OnClickCancel() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.PriceSelection);
        };
    }
}
