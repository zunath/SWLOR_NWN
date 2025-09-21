using System.Collections.Generic;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Models;
using SWLOR.Shared.Dialog.Service;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class TomeItemDefinition: IItemListDefinition
    {
        private readonly ICurrencyService _currencyService;
        private readonly IGuiService _guiService;
        private readonly ItemBuilder _builder = new();

        public TomeItemDefinition(ICurrencyService currencyService, IGuiService guiService)
        {
            _currencyService = currencyService;
            _guiService = guiService;
        }

        public Dictionary<string, ItemDetail> BuildItems()
        {
            XPTomes();
            PerkRefundTome();
            StatRefundTome();

            return _builder.Build();
        }

        private void XPTomes()
        {
            _builder.Create("xp_tome_1", "xp_tome_2", "xp_tome_3", "xp_tome_4")
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    SetLocalObject(user, "XP_TOME_OBJECT", item);
                    AssignCommand(user, () => ClearAllActions());

                    Dialog.StartConversation(user, user, nameof(XPTomeDialog));
                });
        }

        private void PerkRefundTome()
        {
            _builder.Create("refund_tome")
                .Delay(3f)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user))
                    {
                        return "Only players may use this item.";
                    }
                    
                    if (_currencyService.GetCurrency(user, CurrencyType.PerkRefundToken) >= 99)
                    {
                        return "You cannot add any more perk refunds to your collection.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    _currencyService.GiveCurrency(user, CurrencyType.PerkRefundToken, 1);
                    SendMessageToPC(user, $"You gain a perk refund token. (Total: {_currencyService.GetCurrency(user, CurrencyType.PerkRefundToken)})");
                    DestroyObject(item);
                    _guiService.PublishRefreshEvent(user, new PerkResetAcquiredRefreshEvent());
                });
        }

        private void StatRefundTome()
        {
            _builder.Create("recond_tome")
                .Delay(3f)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user))
                    {
                        return "Only players may use this item.";
                    }

                    if (_currencyService.GetCurrency(user, CurrencyType.StatRefundToken) >= 99)
                    {
                        return "You cannot add any more stat refunds to your collection.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    _currencyService.GiveCurrency(user, CurrencyType.StatRefundToken, 1);
                    SendMessageToPC(user, $"You gain a stat refund token. (Total: {_currencyService.GetCurrency(user, CurrencyType.StatRefundToken)})");
                    DestroyObject(item);
                });
        }

    }
}
