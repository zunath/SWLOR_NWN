/**********************************/
/*          d1_cardwom1_a1
/**********************************/
/*  Selects deck type for NPC.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    int nRnd = d4();
    int nDeck = (nRnd <= 3) ? CARD_DECK_TYPE_ANGELS : CARD_DECK_TYPE_SPELLS;

    SetNPCCardAI (CARD_AI_DIFFICULTY_HARD);
    SetNPCDeckType (nDeck);
    SetNPCGoldBet (0);
    SetNPCAnteBet(TRUE);
}

