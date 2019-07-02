/**********************************/
/*           d1_cardman2_a2
/**********************************/
/*  Selects deck type for NPC.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    int nRnd = d4();
    int nDeck = (nRnd <= 3) ? CARD_DECK_TYPE_BIG_CREATURES : CARD_DECK_TYPE_UNDEAD;

    SetNPCCardAI (CARD_AI_DIFFICULTY_HARD);
    SetNPCDeckType (nDeck);
    SetNPCGoldBet (200);
    SetNPCAnteBet(FALSE);
}
