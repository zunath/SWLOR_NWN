/**********************************/
/*           d1_cardman3_a2
/**********************************/
/*  Selects deck type for NPC.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    int nRnd = d4();
    int nDeck = (nRnd <= 3) ? CARD_DECK_TYPE_FAST_CREATURES : CARD_DECK_TYPE_KOBOLDS;

    SetNPCCardAI (CARD_AI_DIFFICULTY_HARD);
    SetNPCDeckType (nDeck);
    SetNPCGoldBet (100);
    SetNPCAnteBet(FALSE);
}

