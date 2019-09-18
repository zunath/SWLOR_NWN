using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject.Dialog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Conversation
{
    class PazaakCollection : ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage();
            DialogPage selectCardPage = new DialogPage("Pick a card, any card...");
            DialogPage selectDeckSlotPage = new DialogPage("Please select a slot in your deck to replace.");
            
            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("SelectCardPage", selectCardPage);
            dialog.AddPage("SelectDeckSlotPage", selectDeckSlotPage);

            return dialog;
        }

        public override void Initialize()
        {
            LoadMainPage();
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            if (pageName == "MainPage") MainPageResponses(responseID);
            else if (pageName == "SelectCardPage") SelectCardPageResponses(responseID);
            else if (pageName == "SelectDeckSlotPage") SelectDeckSlotPageResponses(responseID);
        }

        public override void Back(NWPlayer player, string previousPageName, string currentPageName)
        {
        }


        private void LoadMainPage()
        {
            NWItem collection = GetPC().GetLocalObject("ACTIVE_COLLECTION");
            Dictionary<int, int> collectedCards = new Dictionary<int, int>();
            Dictionary<int, int> deckCards = new Dictionary<int, int>();

            string mainHeader = "Cards in your deck:";
            ClearPageResponses("MainPage");
            ClearPageResponses("SelectDeckSlotPage");

            // allow up to 100 cards in a collection.  That should be plenty, right?
            for (int ii = 1; ii <= 100; ii++)
            {
                int card = PazaakService.GetCardInCollection(ii, collection);

                if (card != 0) collectedCards.Add(ii, card);
            }

            for (int jj = 1; jj<=10; jj++) 
            {
                int deckCard = PazaakService.GetCardInDeck(jj, collection);
                deckCards.Add(jj, collectedCards[deckCard]);
                mainHeader += " " + PazaakService.Display(collectedCards[deckCard]) + " ";
                AddResponseToPage("SelectDeckSlotPage", "Slot " + jj + " (" + PazaakService.Display(collectedCards[deckCard]) + ")", true, jj);
                collectedCards.Remove(deckCard);
            }

            mainHeader += "\n\nCards in your sideboard:";

            List<int> sideboard = collectedCards.Keys.ToList();

            foreach (int card in sideboard)
            {
                mainHeader += " " + PazaakService.Display(collectedCards[card]) + " ";
            }

            if (sideboard.Count == 0) mainHeader += "\n\nFind more cards and add them to your collection for to be able to customise your deck.";
            
            SetPageHeader("MainPage", mainHeader);
                       
            // If we have a sideboard, allow the user to swap cards or remove cards.  Pass the sideboard 
            AddResponseToPage("MainPage", "Swap card from sideboard to deck", sideboard.Count > 0, sideboard);
            AddResponseToPage("MainPage", "Remove card from collection", sideboard.Count > 0, sideboard);
        }

        private void MainPageResponses(int responseID)
        {
            var response = GetResponseByID("MainPage", responseID);
            NWItem collection = GetPC().GetLocalObject("ACTIVE_COLLECTION");
            List<int> sideboard = (List<int>)GetResponseByID("MainPage", responseID).CustomData;

            if (response.Text == "Swap card from sideboard to deck")
            {
                GetPC().SetLocalInt("PAZAAK_ACTION", 1);
                ChangePage("SelectCardPage");
            }
            else if (response.Text == "Remove card from collection")
            {
                GetPC().SetLocalInt("PAZAAK_ACTION", 2);
                ChangePage("SelectCardPage");
            }

            // Build the responses for the select card page.
            ClearPageResponses("SelectCardPage");

            foreach (int card in sideboard)
            {
                AddResponseToPage("SelectCardPage", PazaakService.Display(PazaakService.GetCardInCollection(card, collection)), true, card);
            }
        }

        private void SelectCardPageResponses(int responseID)
        {
            int card = Convert.ToInt32(GetResponseByID("SelectCardPage", responseID).CustomData.ToString());

            if (GetPC().GetLocalInt("PAZAAK_ACTION") == 1)
            {
                ChangePage("SelectDeckSlotPage");
                GetPC().SetLocalInt("PAZAAK_CARD_SELECTED", card);
            }
            else
            {
                NWItem collection = GetPC().GetLocalObject("ACTIVE_COLLECTION");
                PazaakService.RemoveCardFromCollection(card, collection);

                // Rebuild the main page as the collection has changed.
                LoadMainPage();
                ChangePage("MainPage");
            }
        }

        private void SelectDeckSlotPageResponses(int responseID)
        {
            int card = GetPC().GetLocalInt("PAZAAK_CARD_SELECTED");
            int slot = Convert.ToInt32(GetResponseByID("SelectDeckSlotPage", responseID).CustomData.ToString());
            NWItem collection = GetPC().GetLocalObject("ACTIVE_COLLECTION");

            PazaakService.AddCardToDeck(card, collection, slot);

            // Rebuild the main page as the collection has changed.
            LoadMainPage();
            ChangePage("MainPage");
        }

        public override void EndDialog()
        {
        }
    }
}
