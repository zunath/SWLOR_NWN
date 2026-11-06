using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PazaakService;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    /// <summary>
    /// Using a Pazaak card item adds one copy of its side card to the player's collection.
    /// </summary>
    public class PazaakCardItemDefinition : IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            PazaakCard();

            return _builder.Build();
        }

        private void PazaakCard()
        {
            _builder.Create(PazaakCards.CardItemTag)
                .Delay(1f)
                .ReducesItemCharge()
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user) || GetIsDMPossessed(user))
                        return "Only player characters can collect Pazaak cards.";

                    var type = (PazaakCardType)GetLocalInt(item, PazaakCards.CardTypeVariable);
                    if (!PazaakCards.IsValid(type))
                    {
                        Log.Write(LogGroup.Error, $"{PazaakCards.CardTypeVariable} for item '{GetResRef(item)}' is not a valid Pazaak card.");
                        return "This card is not configured properly. Notify an admin.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var type = (PazaakCardType)GetLocalInt(item, PazaakCards.CardTypeVariable);
                    var owned = Pazaak.AddCardToCollection(user, type);
                    var name = PazaakCards.Get(type).Name;
                    SendMessageToPC(user, ColorToken.Green($"You add the {name} card to your Pazaak collection. You can now put {owned} in your side deck."));
                });
        }
    }
}
