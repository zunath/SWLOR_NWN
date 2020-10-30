using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript;

/**
 * PazaakService
 * - Handles functions for collecting Pazaak cards and selecting them for a deck.
 * - Handles functions for starting and finishing a game, and tracking active games. 
 * 
 * _Card collections_
 * A player's card collection is represented as a single item in their inventory.  This has (currently) one deck, and a sideboard of unused cards.
 * Cards are acquired as separate items and "used" on the collection to add them to it.  The collection may be "used" to take one or more cards out.
 * The collection starts with a basic legal deck of 10 cards and no sideboard.  Unused cards may be swapped in to the deck in any of the 10 slots.
 * 
 * _Pazaak gameplay_
 * A Pazaak game deck has 40 cards, 10 of each value 1-10.  The objective is to get as close to 20 as possible without going over.  Players start
 * by drawing 4 cards at random from their side deck.
 * 
 * First player is chosen by a random draw from the deck.  These cards are then set aside.
 * Each player in turn, then
 * - Draws a card from the main deck.
 * - May play a card from their hand. 
 * - Decide to either End Turn or Stand.  
 *   - End Turn means you will draw in your next turn, Stand means you keep your current score. 
 *   - Hitting 20 = auto stand.
 *   - Going over 20 = bust = auto lose.
 * 
 * Repeat in turn until one player wins the set.  The winner of the set plays first in the next set.  Best of 5 sets, hand cards are not refreshed between sets.
 * If a set is tied, the set is not counted and restarts, switching first player. 
 * 
 * Other specials:
 * - 9 cards on the table without being bust = win.
 * 
 * _Other files_
 * ValueObject.PazaakGame - records information about the deck
 * Conversation.PazaakCollection - for managing a player's collection
 * Conversation.PazaakTable - for playing an actual game. 
 * 
 * Use the PazaakCollection item on yourself to manage your deck.
 * Use the PazaakCollection item on a PazaakTable to join a game.  The convo will ask whether you're playing against an NPC or waiting for another player to join. 
 */

namespace SWLOR.Game.Server.Service
{
    class PazaakService
    {
        private static Dictionary<NWObject, PazaakGame> ActiveGames;

        public static int AddCardToCollection(NWItem card, NWObject collection)
        {
            // Adds the card represented by card to collection, destroying the card item and returning its new index in the collection. 
            var CardType = card.GetLocalInt("PAZAAK_CARD_TYPE");

            // Find the first available slot in the collection. 
            var index = 1;
            while (collection.GetLocalInt("CARD_" + index) != 0)
            {
                index++;
            }

            collection.SetLocalInt("CARD_" + index, CardType);
            card.Destroy();
            
            return index;
        }

        public static void RemoveCardFromCollection(int index, NWItem collection)
        {
            // removes the card at index card, creating a card object with the right variable in the player's inventory. 
            // First check that the card is not in the deck.
            for (var ii = 1; ii <= 10; ii++)
            {
                var cardInDeck = collection.GetLocalInt("DECK_" + ii);
                if (cardInDeck == index)
                {
                    NWScript.SendMessageToPC(NWScript.GetItemPossessor(collection), "You cannot remove a card that's in your play deck.");
                    return;
                }
            }

            // All good - extract it.
            NWItem card = NWScript.CreateItemOnObject("pazaakcard", NWScript.GetItemPossessor(collection));
            card.SetLocalInt("PAZAAK_CARD_TYPE", collection.GetLocalInt("CARD_" + index));
            card.Name = "Pazaak Card (" + Display(collection.GetLocalInt("CARD_" + index)) + ")";
            collection.DeleteLocalInt("CARD_" + index);
            return;
        }

        public static void AddCardToDeck(int collectionIndex, NWItem collection, int deckIndex)
        {
            // Adds the card at position card in collection to slot in the collection's active deck.
            // First check that the card is not in the deck.
            for (var ii = 1; ii <= 10; ii++)
            {
                var cardInDeck = collection.GetLocalInt("DECK_" + ii);
                if (cardInDeck == collectionIndex)
                {
                    NWScript.SendMessageToPC(NWScript.GetItemPossessor(collection), "You cannot add a card that's already in your play deck.");
                    return;
                }
            }

            collection.SetLocalInt("DECK_" + deckIndex, collectionIndex);
        }

        public static int GetCardInCollection(int index, NWItem collection)
        {
            return collection.GetLocalInt("CARD_" + index);
        }

        public static int GetCardInDeck(int index, NWItem collection)
        {
            // Note - the deck stores indices into the collection, not values.  Call GetCardInCollection on the returned index to get the actual value.
            return collection.GetLocalInt("DECK_" + index);
        }

        public static PazaakGame GetCurrentGame(NWObject table)
        {
            // Return the game currently being played by player.  This allows them to be interrupted while playing and resume seamlessly. 
            if (ActiveGames == null) ActiveGames = new Dictionary<NWObject, PazaakGame>();

            if (!ActiveGames.ContainsKey(table))
                return null;

            return ActiveGames[table];
        }

        public static PazaakGame StartGame(NWObject table, NWObject player1, NWObject player2)
        {
            // Begin a game.  player1 and player2 can be PCs or NPCs (or DMPCs).
            var game = new PazaakGame(player1, player2);

            if (ActiveGames == null) ActiveGames = new Dictionary<NWObject, PazaakGame>();

            ActiveGames[player1] = game;
            ActiveGames[player2] = game;
            ActiveGames[table] = game;
            return game;
        }

        public static void EndGame(NWObject table, PazaakGame game)
        {
            if (ActiveGames == null) ActiveGames = new Dictionary<NWObject, PazaakGame>();
            ActiveGames.Remove(game.player1);
            ActiveGames.Remove(game.player2);
            ActiveGames.Remove(table);           
        }

        public static string Display(int cardType)
        {
            switch (cardType)
            {
                case 1: return "+1";
                case 2: return "+2";
                case 3: return "+3";
                case 4: return "+4";
                case 5: return "+5";
                case 6: return "+6";
                case -1: return "-1";
                case -2: return "-2";
                case -3: return "-3";
                case -4: return "-4";
                case -5: return "-5";
                case -6: return "-6";
                case 101: return "+-1";
                case 102: return "+-2";
                case 103: return "+-3";
                case 104: return "+-4";
                case 105: return "+-5";
                case 106: return "+-6";
                case 201: return "Flip";
                case 202: return "Double";
                case 203: return "Tiebreaker";
            }

            return "Error!";
        }
    }
}
