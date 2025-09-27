using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Inventory.Dialog;
using SWLOR.Component.Inventory.Service;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.ValueObjects;

namespace SWLOR.Component.Inventory.Feature.ItemDefinition
{
    public class DestroyItemDefinition: IItemListDefinition
    {
        private readonly IServiceProvider _serviceProvider;
        private IItemBuilder Builder => _serviceProvider.GetRequiredService<IItemBuilder>();

        public DestroyItemDefinition(
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded service to break circular dependency
        private IDialogService DialogService => _serviceProvider.GetRequiredService<IDialogService>();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            Builder.Create("player_guide", "survival_knife")
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    SetLocalObject(user, "DESTROY_ITEM", item);
                    DialogService.StartConversation(user, user, nameof(DestroyItemDialog));
                });

            return Builder.Build();
        }
    }
}
