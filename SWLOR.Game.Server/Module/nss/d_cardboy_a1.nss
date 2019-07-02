/**********************************/
/*          d_cardboy_a1
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/**********************************/
/*  Sets up NPC information.  NPC
/*  Deck remains same for 3/4 time.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    int nRnd = d4();
    int nDeck = (nRnd <= 3) ? CARD_DECK_TYPE_GOBLINS : CARD_DECK_TYPE_WOLVES;

    SetNPCCardAI (CARD_AI_DIFFICULTY_EASY);
    SetNPCDeckType (nDeck);
    SetNPCGoldBet (0);
    SetNPCAnteBet(FALSE);
}
