using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.ItemService;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class TomeItemDefinition: IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

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
                    
                    if (Currency.GetCurrency(user, CurrencyType.PerkRefundToken) >= 99)
                    {
                        return "You cannot add any more perk refunds to your collection.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    Currency.GiveCurrency(user, CurrencyType.PerkRefundToken, 1);
                    SendMessageToPC(user, $"You gain a perk refund token. (Total: {Currency.GetCurrency(user, CurrencyType.PerkRefundToken)})");
                    DestroyObject(item);
                    Gui.PublishRefreshEvent(user, new PerkResetAcquiredRefreshEvent());
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

                    if (Currency.GetCurrency(user, CurrencyType.StatRefundToken) >= 99)
                    {
                        return "You cannot add any more stat refunds to your collection.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    Currency.GiveCurrency(user, CurrencyType.StatRefundToken, 1);
                    SendMessageToPC(user, $"You gain a stat refund token. (Total: {Currency.GetCurrency(user, CurrencyType.StatRefundToken)})");
                    DestroyObject(item);
                });
        }

    }
}
