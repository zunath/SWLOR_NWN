using System.Collections.Generic;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.ItemService;
using Dialog = SWLOR.Game.Server.Service.Dialog;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class DestroyItemDefinition: IItemListDefinition
    {
        private readonly ItemBuilder _builder = new ItemBuilder();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            _builder.Create("player_guide", "survival_knife")
                .ApplyAction((user, item, target, location) =>
                {
                    SetLocalObject(user, "DESTROY_ITEM", item);
                    Dialog.StartConversation(user, user, nameof(DestroyItemDialog));
                });

            return _builder.Build();
        }
    }
}
