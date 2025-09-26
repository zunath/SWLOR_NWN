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
        private readonly IItemBuilder _builder;


        public DestroyItemDefinition(
            IServiceProvider serviceProvider,
            IItemBuilder itemBuilder)
        {
            _serviceProvider = serviceProvider;
            _builder = itemBuilder;
        }

        // Lazy-loaded service to break circular dependency
        private IDialogService DialogService => _serviceProvider.GetRequiredService<IDialogService>();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            _builder.Create("player_guide", "survival_knife")
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    SetLocalObject(user, "DESTROY_ITEM", item);
                    DialogService.StartConversation(user, user, nameof(DestroyItemDialog));
                });

            return _builder.Build();
        }
    }
}
