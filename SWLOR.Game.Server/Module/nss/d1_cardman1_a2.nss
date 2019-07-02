/**********************************/
/*           d1_cardman1_a2
/**********************************/
/*  Selects deck type for NPC.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    int nRnd = d4();
    int nDeck = (nRnd <= 3) ? CARD_DECK_TYPE_SPELLS : CARD_DECK_TYPE_RATS;

    SetNPCCardAI (CARD_AI_DIFFICULTY_HARD);
    SetNPCDeckType (nDeck);
    SetNPCGoldBet (100);
    SetNPCAnteBet(FALSE);
}
