/**********************************/
/*          d1_cardgame_pvp
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 25th February 2003
/**********************************/
/*  Looks to see if a card game is
/*  to be played between two
/*  players.
/**********************************/

#include "d1_cards_jinc"

void PrepGame(object oPlayer, object oOpponent, int nRules);

void main()
{
    object oPlayer = GetPCSpeaker();

    // make sure they're still accepting wagers
    if(!(GetLocalInt(oPlayer, "WAGER") > 0))
      return;

    int nSelection = GetMenuSelection();
    object oDeck = GetMenuObjectValue (nSelection - 1);
    SetLocalObject(oPlayer, "DECK_SELECTED", oDeck);


    object oOpponent = GetLocalObject(oPlayer, "CARD_OPPONENT");

    int nGold = GetLocalInt(oPlayer, "WAGER_GOLD");
    int nAnte = GetLocalInt(oPlayer, "WAGER_ANTE");
    SetNPCGoldBet(nGold, oPlayer);
    SetNPCGoldBet(nGold, oOpponent);

    // clear out variables
   DeleteLocalObject(oPlayer, "CARD_OPPONENT");
   DeleteLocalInt(oPlayer, "WAGER");
   DeleteLocalInt(oPlayer, "WAGER_ANTE");
   DeleteLocalInt(oPlayer, "WAGER_GOLD");
   DeleteLocalObject(oOpponent, "CARD_OPPONENT");


    int nRules = GetDefaultRules();

    if (nRules & CARD_RULE_LAST_DRAW_CONTINUE)
        nRules -= CARD_RULE_LAST_DRAW_CONTINUE;

    if (nRules & CARD_RULE_TRADE_ALL)
        nRules -= CARD_RULE_TRADE_ALL;

    if (nRules & CARD_RULE_TRADE_ONE)
        nRules -= CARD_RULE_TRADE_ONE;


    if(nAnte > 0)
      nRules = nRules + CARD_RULE_TRADE_ONE;

  AssignCommand(oPlayer, ClearAllActions());

  DelayCommand(3.0, PrepGame(oPlayer, oOpponent, nRules));

}

void PrepGame(object oPlayer, object oOpponent, int nRules)
{


    if( (oPlayer!= OBJECT_INVALID) && (oOpponent != OBJECT_INVALID))
    {
      // check if somethings gone wrong and they're already in the game.
      if(GetTag(GetArea(oPlayer)) == "CardGame")
        return;

      object oPlayerDeck = GetLocalObject(oPlayer, "DECK_SELECTED");
      object oOpponentDeck = GetLocalObject(oOpponent, "DECK_SELECTED");
      if( (oPlayerDeck != OBJECT_INVALID) && (oOpponentDeck != OBJECT_INVALID))
      {
        ActionStartCardGame (oPlayer, oOpponent, nRules);
      }else{
        SendMessageToPC(oPlayer, "Invalid deck.");
        SendMessageToPC(oOpponent, "Invalid deck.");
      }
    }else{
      SendMessageToPC(oPlayer, "Invalid player.");
      SendMessageToPC(oOpponent, "Invalid player.");
    }
}
