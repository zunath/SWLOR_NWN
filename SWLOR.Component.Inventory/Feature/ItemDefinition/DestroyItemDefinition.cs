using SWLOR.Component.Inventory.Contracts;
using SWLOR.Component.Inventory.Dialog;
using SWLOR.Component.Inventory.Model;
using SWLOR.Component.Inventory.Service;
using SWLOR.Shared.Dialog.Contracts;

namespace SWLOR.Component.Inventory.Feature.ItemDefinition
{
    public class DestroyItemDefinition: IItemListDefinition
    {
        private readonly IDialogService _dialog;
        private readonly IItemBuilder _builder;


        public DestroyItemDefinition(
            IDialogService dialogService,
            IItemBuilder itemBuilder)
        {
            _dialog = dialogService;
            _builder = itemBuilder;
        }

        public Dictionary<string, ItemDetail> BuildItems()
        {
            _builder.Create("player_guide", "survival_knife")
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    SetLocalObject(user, "DESTROY_ITEM", item);
                    _dialog.StartConversation(user, user, nameof(DestroyItemDialog));
                });

            return _builder.Build();
        }
    }
}
