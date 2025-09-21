using System.Collections.Generic;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Models;
using Dialog = SWLOR.Shared.Dialog.Service.Dialog;

namespace SWLOR.Game.Server.Feature.ItemDefinition
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
                    Dialog.StartConversation(user, user, nameof(DestroyItemDialog));
                });

            return _builder.Build();
        }
    }
}
