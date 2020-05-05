using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Dialog;
using System;
using System.Collections.Generic;
using static NWN._;

namespace SWLOR.Game.Server.Conversation
{
    class PazaakTable : ConversationBase
    {
        PazaakGame game;

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage();

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadMainPage();
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            MainPageResponses(responseID);
        }

        public override void Back(NWPlayer player, string previousPageName, string currentPageName)
        {
        }


        private void LoadMainPage()
        {
            NWObject table = _.OBJECT_SELF;
            NWPlayer pc = GetPC();
            game = PazaakService.GetCurrentGame(table);

            if (game == null && table.GetLocalInt("IN_GAME") == 2)
            {
                // Belt and bracers clean up code.
                table.DeleteLocalInt("IN_GAME");
            }

            // Check whether we have a game, or whether we should create one.
            if (table.GetLocalInt("IN_GAME") == 2)
            {
                CheckEndRound(table);

                if (game.nextTurn == pc)
                {
                    // Our turn.
                    BuildTurnOptions(pc);
                }
                else if (game.player2.IsNPC)
                {
                    // NPC turn.  Handle some 
                    DoNPCTurn(game);
                    CheckEndRound(table);
                    if (game.nextTurn == game.player2) DoNPCTurn(game);
                    BuildTurnOptions(pc);
                }
                else
                {
                    // Other player turn.
                    SetPageHeader("MainPage", "It is not currently your turn.");
                    ClearPageResponses("MainPage");
                }
            }
            // Table is open for a second player.
            else if (table.GetLocalInt("IN_GAME") == 1)
            {
                NWObject collection = pc.GetLocalObject("ACTIVE_COLLECTION");

                if (GetName(table.GetLocalObject("PLAYER_1")) == pc.Name)
                {
                    SetPageHeader("MainPage", "You are waiting for an opponent here.  Or play against the host.");
                    ClearPageResponses("MainPage");
                    AddResponseToPage("MainPage", "Table host (NPC)");
                }
                // Check that the player has an active Pazaak deck.
                else if (collection.IsValid)
                {
                    SetPageHeader("MainPage", GetName(table.GetLocalObject("PLAYER_1")) + " is waiting for an opponent.  Join game?");
                    ClearPageResponses("MainPage");
                    AddResponseToPage("MainPage", "Join game");
                }
                else
                {
                    SetPageHeader("MainPage", "Use your Pazaak Collection on this table to join the game.");
                    ClearPageResponses("MainPage");
                }
            }
            // Create a game.  Offer the PC a choice of vs NPC or vs Player.
            else
            {
                NWObject collection = pc.GetLocalObject("ACTIVE_COLLECTION");
                // Check that the player has an active Pazaak deck.
                if (collection.IsValid)
                {
                    table.SetLocalInt("IN_GAME", 1);
                    table.SetLocalObject("PLAYER_1", pc);
                    table.DeleteLocalObject("PLAYER_2");

                    SetPageHeader("MainPage", "Game created.  Will this be against another player, or the table owner?");
                    ClearPageResponses("MainPage");
                    AddResponseToPage("MainPage", "A player");
                    AddResponseToPage("MainPage", "Table host (NPC)");
                }
                else
                {
                    SetPageHeader("MainPage", "Use your Pazaak Collection on this table to join the game.");
                    ClearPageResponses("MainPage");
                }
            }
        }

        private void BuildTurnOptions(NWPlayer pc)
        {
            // Draw a card.
            int score = pc == game.player1 ? game.player1Score : game.player2Score;

            int card = game.DrawCard();
            score += card;
            game.lastCardPlayed = card;
            pc.FloatingText("You drew a " + card + " putting your score at " + score);
            if (pc == game.player1)
            {
                game.player1Score = score;
            }
            else
            {
                game.player2Score = score;
            }

            // Since we're having a turn, we can't be the one standing... 
            bool bStand = game.player1Standing || game.player2Standing;

            string header = "Your score is at " + score + ". " + (bStand ? "The other player is standing. " : "") +
                "What do you want to do?";
            SpeakString(pc.Name + " draws a " + card + " from the deck.  Their score is now " + score);

            if (pc.GetLocalInt("PAZAAK_REMINDED") == 0)
            {
                pc.SendMessage("(Pazaak Rules Reminder: highest score under 21 wins, ending your turn at 21+ loses you the round.  You may play up to one card from your hand each turn. A standing player does not get to play again, but ending your turn means you will have to draw at least one more card.)");
                pc.SetLocalInt("PAZAAK_REMINDED", 1);
            }

            SetPageHeader("MainPage", header);
            ClearPageResponses("MainPage");

            List<int> sideDeck = pc == game.player1 ? game.player1SideDeck : game.player2SideDeck;

            // Build options from the side deck.
            foreach (int sideCard in sideDeck)
            {
                if ((sideCard > 100 && sideCard < 107) || sideCard == 203)
                {
                    // Card can be played either of two ways. 
                    AddResponseToPage("MainPage", "Play card from side deck: " + PazaakService.Display(sideCard) + " as positive", true, sideCard);
                    AddResponseToPage("MainPage", "Play card from side deck: " + PazaakService.Display(sideCard) + " as negative", true, sideCard);
                }
                else
                {
                    AddResponseToPage("MainPage", "Play card from side deck: " + PazaakService.Display(sideCard), true, sideCard);
                }
            }

            AddResponseToPage("MainPage", "End Turn", score < 21);
            AddResponseToPage("MainPage", "Stand");
        }

        private void MainPageResponses(int responseID)
        {
            var response = GetResponseByID("MainPage", responseID);
            NWObject table = _.OBJECT_SELF;
            NWPlayer pc = GetPC();
            game = PazaakService.GetCurrentGame(table);

            if (response.Text == "A player")
            {
                // Leave table open for PC to join.
                EndConversation();
            }
            else if (response.Text == "Table host (NPC)")
            {
                NWCreature NPC = GetNearestCreature(CREATURE_TYPE_IS_ALIVE, TRUE, pc.Object, 1, CREATURE_TYPE_PLAYER_CHAR, FALSE); 

                if (NPC.IsValid)
                {
                    AssignCommand(NPC, () => { ActionMoveToObject(table); });
                    game = PazaakService.StartGame(table, pc, NPC);
                    table.SetLocalInt("IN_GAME", 2);

                    if (game.nextTurn == game.player2)
                    {
                        DelayCommand(2.0f, () => { DoNPCTurn(game); });
                    }
                }
                else
                {
                    pc.SendMessage("Sorry, this table has no host.");
                }

                EndConversation();
            }
            else if (response.Text == "Join game")
            {
                NWObject p1 = table.GetLocalObject("PLAYER_1");
                PazaakService.StartGame(table, p1, pc);
                table.SetLocalInt("IN_GAME", 2);

                EndConversation();
            }
            else if (response.Text.StartsWith("Play card from side deck"))
            {
                // Get the card value and modify the score.  Then rebuild the options. 
                int card = Convert.ToInt32(GetResponseByID("MainPage", responseID).CustomData.ToString());
                string cardText = "You play a " + PazaakService.Display(card);

                if (pc == game.player1) game.player1SideDeck.Remove(card);
                else game.player2SideDeck.Remove(card);

                if (card > 100 && card < 107)
                {
                    // Make it a 1-6 number.
                    card -= 100;

                    if (response.Text.EndsWith("negative"))
                    {
                        // Played as negative, so flip the sign. 
                        card *= -1;
                    }
                }

                if (card > 200)
                {
                    switch (card)
                    {
                        case 201: // Flip
                        {
                            card = (pc == game.player1 ? game.player1HandCardsPlayedThisSet : game.player2HandCardsPlayedThisSet) * -2;
                            break;
                        }
                        case 202: // Double
                        {
                            card = game.lastCardPlayed;
                            break;
                        }
                        case 203: // Tiebreaker
                        {
                            card = 1;
                            if (response.Text.EndsWith("negative"))
                            {
                                // Played as negative, so flip the sign. 
                                card *= -1;
                            }
                            game.tiebreaker = pc;
                            break;
                        }
                    }
                }

                int score;
                if (pc == game.player1)
                {
                    game.player1Score += card;
                    game.player1HandCardsPlayedThisSet += card;
                    score = game.player1Score;
                }
                else
                {
                    game.player2Score += card;
                    game.player2HandCardsPlayedThisSet += card;
                    score = game.player2Score;
                }

                pc.FloatingText(cardText + ". Your score is now " + score);
                SpeakString(pc.Name + " plays a " + cardText + ", taking their score to " + score);

                // Since we're having a turn, we can't be the one standing... 
                bool bStand = game.player1Standing || game.player2Standing;

                string header = "Your score is at " + score + ". " + (bStand ? "The other player is standing. " : "") +
                    "What do you want to do?";

                SetPageHeader("MainPage", header);
                ClearPageResponses("MainPage");
                AddResponseToPage("MainPage", "End Turn", score < 21);
                AddResponseToPage("MainPage", "Stand");
            }
            else if (response.Text == "End Turn" || response.Text == "Stand")
            {
                // Whatever happens, end the dialog.
                EndConversation();

                // Process end turn responses, then do NPC turn if it's an NPC game.
                if (response.Text == "Stand")
                {
                    if (pc == game.player1) game.player1Standing = true;
                    else game.player2Standing = true;
                }

                // Record that it is time for the next player to take their turn, unless they are standing. 
                if (pc == game.player1 && !game.player2Standing) game.nextTurn = game.player2;
                else if (pc == game.player2 && !game.player1Standing) game.nextTurn = game.player1;

                // If both players are PCs, we shouldn't have to do anything more now.  But if we're playing with an NPC, now the NPC needs 
                // to take their turn. 
                float delay = 1.0f;

                if (!CheckEndRound(table) && game.player2.IsNPC && !game.player2Standing)
                {
                    game = DoNPCTurn(game);

                    while (game.player2.IsNPC && game.player1Standing && !game.player2Standing && !CheckEndRound(table, delay))
                    {
                        // PC is standing, so keep playing NPC turns until the round ends. 
                        game = DoNPCTurn(game, delay);
                        delay += 1.0f;
                    }
                }

                CheckEndRound(table, delay);
            }
        }

        private PazaakGame DoNPCTurn(PazaakGame game, float delay = 0.0f)
        {
            int card = game.DrawCard();
            game.player2Score += card;
            int score = game.player2Score;
            DelayCommand(0.5f + delay, () => { SpeakString(GetName(game.player2) + " draws a " + card + " from the deck.  Their score is now " + score); });

            // Decide what to do next.
            // If higher than 20, see if any cards in our hand will rescue us.
            // If exactly 20, stand.
            // If the PC has stood and we are higher, stand.
            // If we have a card that will put us to 20, play it.
            // If the PC has stood and we have a card that will put us above them, play it.
            // If we have 18 or more points (and aren't losing), stand.
            // Else end turn. 
            bool bStand = false;

            if (score > 20) PlayNPCCard(delay);
            else if (score == 20) bStand = true;
            else if (game.player1Standing && score > game.player1Score) bStand = true;
            else if (game.player1Standing && PlayNPCCard(delay) > game.player1Score) bStand = true;
            else if (PlayNPCCard(delay) == 20) bStand = true;

            if (game.player2Score >= 18 && game.player2Score >= game.player1Score) bStand = true;

            if (bStand) game.player2Standing = true;
            if (!game.player1Standing) game.nextTurn = game.player1;

            return game;
        }

        private int PlayNPCCard(float delay)
        {
            // We'll call this method to do one of three things.
            // - If our score is over 20, rescue it to sub 20.
            // - If playing a card would put us over a standing player's score, play it.
            // - If playing a card would put us to exactly 20, play it. 
            // NPCs will never have the "special" cards (200+). 
            if (game.player2Score > 20)
            {
                foreach (int card in game.player2SideDeck)
                {
                    int adjust = 0;
                    if (card > 0 && card < 7) continue;
                    if (card > 100 && card < 107) adjust = -1 * (card - 100);
                    if (card < 0) adjust = card;

                    if (game.player2Score + adjust < 21)
                    {
                        game.player2Score += adjust;
                        int score = game.player2Score;
                        DelayCommand(1.0f + delay, () => { SpeakString(GetName(game.player2) + " plays " + PazaakService.Display(card) + " from hand.  Score is now " + score); });
                        game.player2SideDeck.Remove(card);
                        break;
                    }
                }
            }
            else
            {
                int targetScore = game.player1Standing ? game.player1Score : 19;

                foreach (int card in game.player2SideDeck)
                {
                    int adjust = 0;
                    if (card > 0 && card < 7) adjust = card;
                    if (card > 100 && card < 107) adjust = card - 100;
                    if (card < 0) continue;

                    if (game.player2Score + adjust < 21 && game.player2Score + adjust > targetScore)
                    {
                        game.player2Score += adjust;
                        int score = game.player2Score;
                        DelayCommand(1.0f + delay, () => { SpeakString(GetName(game.player2) + " plays " + PazaakService.Display(card) + " from hand.  Score is now " + score); });
                        game.player2SideDeck.Remove(card);
                        break;
                    }
                }
            }

            return game.player2Score;
        }

        private bool CheckEndRound(NWObject table, float delay = 0.0f)
        {
            // SpeakString("DEBUG: Checking end round.  Player 1 standing? " + game.player1Standing + " Player 2 standing? " + game.player2Standing + " Scores: " + game.player1Score + "/" + game.player2Score);
            // If the game is already over, skip the rest.
            if (table.GetLocalInt("IN_GAME") == 0) return true;

            if (game.player1Score > 20 || game.player2Score > 20 || (game.player1Standing && game.player2Standing))
            {
                game.EndRound();

                if (game.player1Sets == 3)
                {
                    DelayCommand(0.5f + delay, () =>
                    {
                        SpeakString(GetName(game.player1) + " wins, 3 sets to " + game.player2Sets + "!");
                    });
                    PazaakService.EndGame(table, game);
                    table.DeleteLocalInt("IN_GAME");
                }
                else if (game.player2Sets == 3)
                {
                    DelayCommand(1.5f + delay, () =>
                    {
                        SpeakString(GetName(game.player2) + " wins, 3 sets to " + game.player1Sets + "!");
                    });
                    PazaakService.EndGame(table, game);
                    table.DeleteLocalInt("IN_GAME");
                }
                else
                {
                    DelayCommand(1.5f + delay, () =>
                    {
                        SpeakString("New set beginning.  " + GetName(game.player1) + " has won " + game.player1Sets + " sets, " +
   GetName(game.player2) + " has won " + game.player2Sets + " sets. " + GetName(game.nextTurn) + " to play.");
                    });
                }

                return true;
            }

            return false;
        }

        public override void EndDialog()
        {
        }
    }
}
