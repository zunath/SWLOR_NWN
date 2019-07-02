/**********************************/
/*           d1_cardgirl_a2
/**********************************/
/*  Selects deck type for NPC.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    int nRnd = d4();
    int nDeck = (nRnd <= 3) ? CARD_DECK_TYPE_WOLVES : CARD_DECK_TYPE_GOBLINS;

    SetNPCCardAI (CARD_AI_DIFFICULTY_NORMAL);
    SetNPCDeckType (nDeck);
    SetNPCAnteBet(TRUE);
}
