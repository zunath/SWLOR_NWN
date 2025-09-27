using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Inventory.Dialog;
using SWLOR.Component.Inventory.Service;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.ValueObjects;
using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Inventory.Feature.ItemDefinition
{
    public class TomeItemDefinition: IItemListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private IItemBuilder Builder => _serviceProvider.GetRequiredService<IItemBuilder>();

        public TomeItemDefinition(
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private ICurrencyService CurrencyService => _serviceProvider.GetRequiredService<ICurrencyService>();
        private IGuiService GuiService => _serviceProvider.GetRequiredService<IGuiService>();
        private IDialogService DialogService => _serviceProvider.GetRequiredService<IDialogService>();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            XPTomes();
            PerkRefundTome();
            StatRefundTome();

            return Builder.Build();
        }

        private void XPTomes()
        {
            Builder.Create("xp_tome_1", "xp_tome_2", "xp_tome_3", "xp_tome_4")
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    SetLocalObject(user, "XP_TOME_OBJECT", item);
                    AssignCommand(user, () => ClearAllActions());

                    DialogService.StartConversation(user, user, nameof(XPTomeDialog));
                });
        }

        private void PerkRefundTome()
        {
            Builder.Create("refund_tome")
                .Delay(3f)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user))
                    {
                        return "Only players may use this item.";
                    }
                    
                    if (CurrencyService.GetCurrency(user, CurrencyType.PerkRefundToken) >= 99)
                    {
                        return "You cannot add any more perk refunds to your collection.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    CurrencyService.GiveCurrency(user, CurrencyType.PerkRefundToken, 1);
                    SendMessageToPC(user, $"You gain a perk refund token. (Total: {CurrencyService.GetCurrency(user, CurrencyType.PerkRefundToken)})");
                    DestroyObject(item);
                    GuiService.PublishRefreshEvent(user, new PerkResetAcquiredRefreshEvent());
                });
        }

        private void StatRefundTome()
        {
            Builder.Create("recond_tome")
                .Delay(3f)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user))
                    {
                        return "Only players may use this item.";
                    }

                    if (CurrencyService.GetCurrency(user, CurrencyType.StatRefundToken) >= 99)
                    {
                        return "You cannot add any more stat refunds to your collection.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    CurrencyService.GiveCurrency(user, CurrencyType.StatRefundToken, 1);
                    SendMessageToPC(user, $"You gain a stat refund token. (Total: {CurrencyService.GetCurrency(user, CurrencyType.StatRefundToken)})");
                    DestroyObject(item);
                });
        }

    }
}
