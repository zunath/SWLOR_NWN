/**********************************/
/*           d1_cardgirl_a1
/**********************************/
/*  Selects deck type for NPC.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    int nRnd = d4();
    int nDeck = (nRnd <= 3) ? CARD_DECK_TYPE_GOBLINS : CARD_DECK_TYPE_RANDOM;

    SetNPCCardAI (CARD_AI_DIFFICULTY_EASY);
    SetNPCDeckType (nDeck);
    SetNPCAnteBet(FALSE);
}
