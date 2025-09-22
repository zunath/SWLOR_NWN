using SWLOR.Component.Inventory.Contracts;
using SWLOR.Component.Inventory.Dialog;
using SWLOR.Component.Inventory.Model;
using SWLOR.Component.Inventory.Service;

namespace SWLOR.Component.Inventory.Feature.ItemDefinition
{
    public class DestroyItemDefinition: IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            _builder.Create("player_guide", "survival_knife")
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    SetLocalObject(user, "DESTROY_ITEM", item);
                    Shared.Dialog.Service.Dialog.StartConversation(user, user, nameof(DestroyItemDialog));
                });

            return _builder.Build();
        }
    }
}
