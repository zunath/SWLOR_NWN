using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.PazaakService;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class PazaakCardItemDefinition : IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            CardUnlockToken();
            return _builder.Build();
        }

        private void CardUnlockToken()
        {
            _builder.Create("PAZAAK_CARD")
                .Delay(1f)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user))
                        return "Only players may use Pazaak cards.";

                    var cardId = GetLocalInt(item, Pazaak.CardItemLocalVariable);
                    var cardType = (PazaakCardType)cardId;
                    if (!PazaakCardCatalog.IsValidCard(cardType))
                        return "This Pazaak card token is misconfigured. Notify an admin.";

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var cardType = (PazaakCardType)GetLocalInt(item, Pazaak.CardItemLocalVariable);
                    Pazaak.GrantCard(user, cardType);
                    SendMessageToPC(user, $"You add {PazaakCardCatalog.GetName(cardType)} to your Pazaak collection.");
                    Item.ReduceItemStack(item, 1);
                });
        }
    }
}
