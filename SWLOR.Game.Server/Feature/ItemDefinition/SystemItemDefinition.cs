using System.Collections.Generic;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class SystemItemDefinition: IItemListDefinition
    {
        public Dictionary<string, ItemDetail> BuildItems()
        {
            var builder = new ItemBuilder();
            DestroyItemConversationItems(builder);

            return builder.Build();
        }

        private void DestroyItemConversationItems(ItemBuilder builder)
        {
            builder.Create("player_guide", "survival_knife")
                .ApplyAction((user, item, target, location) =>
                {
                    Dialog.StartConversation(user, user, nameof(DestroyItemDialog));
                });
        }
    }
}
