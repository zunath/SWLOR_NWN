using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using System.Collections.Generic;
using System.Linq;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

/**
 * Used in Pazaak games, mainly to keep track of the contents of the deck. 
 * TODO: track 9-card victory.
 */
namespace SWLOR.Game.Server.ValueObject
{
    class PazaakGame
    {
        private List<int> deck;

        public readonly NWObject player1;
        public readonly NWObject player2;

        // Track the score this round and across the game.
        public int player1Score;
        public int player2Score;

        public int player1Sets;
        public int player2Sets;

        // Track state within this round. 
        public int player1HandCardsPlayedThisSet = 0;
        public int player2HandCardsPlayedThisSet = 0;
        public int lastCardPlayed = 0;

        public bool player1Standing = false;
        public bool player2Standing = false;

        public NWObject nextTurn;
        public NWObject startedThisRound;

        public List<int> player1SideDeck;
        public List<int> player2SideDeck;

        public NWObject tiebreaker;

        public PazaakGame(NWObject p1, NWObject p2)
        {
            player1 = p1;
            player2 = p2;
            player1Score = 0;
            player2Score = 0;
            
            // Build the game deck.
            deck = new List<int> { 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7, 8, 8, 8, 8, 9, 9, 9, 9, 10, 10, 10, 10 };

            // Draw for first turn.
            while (startedThisRound == null)
            {
                var c1 = DrawCard();
                var c2 = DrawCard();
                FloatingTextStringOnCreature("Draws " + c1 + " for first player (highest plays first)", player1, false);

                if (GetIsPC(player2))
                {
                    FloatingTextStringOnCreature("Draws " + c2 + " for first player (highest plays first)", player2, true);
                }
                else
                {
                    DelayCommand(1.0f, () => { FloatingTextStringOnCreature(player2.Name + " draws " + c2 + " to start", player1, false); });
                }

                if (c1 > c2) startedThisRound = player1;
                else if (c2 > c1) startedThisRound = player2;
            }

            nextTurn = startedThisRound;

            // Build the decks.
            BuildSideDeck(p1);
            BuildSideDeck(p2);
        }

        private void BuildSideDeck(NWObject player)
        {
            NWItem collection = player.GetLocalObject("ACTIVE_COLLECTION");
            var deckToBuild = new List<int>(4);
            var playerDeck = new List<int>(10);

            int random;
            if (collection.IsValid)
            {
                // We have a player with a real deck.  Build the list and select 4 random cards from it. 
                for (var ii = 1; ii <= 10; ii++)
                {
                    playerDeck.Add(PazaakService.GetCardInCollection(PazaakService.GetCardInDeck(ii, collection), collection));
                }
            }
            else
            {
                // This is an NPC.  Give them a deck with 6 random + cards, 3-4 random - cards and 0-1 random wild card.
                // See PazaakCard.cs for values.
                if (RandomService.D6(1) == 6)
                {
                    playerDeck.Add(RandomService.D6(1));
                    playerDeck.Add(RandomService.D6(1));
                    playerDeck.Add(RandomService.D6(1));
                    playerDeck.Add(RandomService.D6(1));
                    playerDeck.Add(RandomService.D6(1));
                    playerDeck.Add(RandomService.D6(1));

                    playerDeck.Add(RandomService.D6(1) * -1);
                    playerDeck.Add(RandomService.D6(1) * -1);
                    playerDeck.Add(RandomService.D6(1) * -1);

                    playerDeck.Add(RandomService.D6(1) + 100);
                }
                else
                {
                    playerDeck.Add(RandomService.D6(1));
                    playerDeck.Add(RandomService.D6(1));
                    playerDeck.Add(RandomService.D6(1));
                    playerDeck.Add(RandomService.D6(1));
                    playerDeck.Add(RandomService.D6(1));
                    playerDeck.Add(RandomService.D6(1));

                    playerDeck.Add(RandomService.D6(1) * -1);
                    playerDeck.Add(RandomService.D6(1) * -1);
                    playerDeck.Add(RandomService.D6(1) * -1);
                    playerDeck.Add(RandomService.D6(1) * -1);
                }
            }

            random = RandomService.Random(10);
            deckToBuild.Add(playerDeck[random]);
            playerDeck.Remove(random);

            random = RandomService.Random(9);
            deckToBuild.Add(playerDeck[random]);
            playerDeck.Remove(random);

            random = RandomService.Random(8);
            deckToBuild.Add(playerDeck[random]);
            playerDeck.Remove(random);

            random = RandomService.Random(7);
            deckToBuild.Add(playerDeck[random]);
            playerDeck.Remove(random);

            if (player1 == player) player1SideDeck = deckToBuild;
            else player2SideDeck = deckToBuild;
        }

        public int DrawCard()
        {
            if (deck.Count == 0)
            {
                deck = new List<int> { 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7, 8, 8, 8, 8, 9, 9, 9, 9, 10, 10, 10, 10 };
                SpeakString("Deck empty! Reshuffling.");
            }

            var card = RandomService.Random(deck.Count);
            var retVal = deck.ElementAt(card);
            deck.RemoveAt(card);
            return retVal;            
        }
        
        public void EndRound()
        {
            // Determine winner.
            if (player1Score > 20 && player2Score > 20)
            {
                // Set is tied.  Switch first player and reset.
                if (startedThisRound == player1) startedThisRound = player2;
                else startedThisRound = player1;
            }
            else if (player1Score > 20)
            {
                player2Sets++;
                startedThisRound = player2;
            }
            else if (player2Score > 20)
            {
                player1Sets++;
                startedThisRound = player1;
            }
            else if (player1Score > player2Score)
            {
                player1Sets++;
                startedThisRound = player1;
            }
            else if (player2Score > player1Score)
            {
                player2Sets++;
                startedThisRound = player2;
            }
            else if (tiebreaker == player1)
            {
                player1Sets++;
                startedThisRound = player1;
            }
            else if (tiebreaker == player2)
            {
                player2Sets++;
                startedThisRound = player2;
            }
            // If none of the above, then the set is tied, and neither player scores.  Switch first player.
            else
            {
                if (startedThisRound == player1) startedThisRound = player2;
                else startedThisRound = player1;
            }

            nextTurn = startedThisRound;
            tiebreaker = null;
            
            player1Score = 0;
            player2Score = 0;

            player1Standing = false;
            player2Standing = false;

            player1HandCardsPlayedThisSet = 0;
            player2HandCardsPlayedThisSet = 0;
            lastCardPlayed = 0;

            // Don't reshuffle the deck - we are now ready for the next round (or if one player has 3 sets, game over).
        }
    }
}
