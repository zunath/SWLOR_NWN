/**********************************/
/*          d1_gameowner_c1
/**********************************/
/*  Checks if the player has cards
/*  in their collection.
/**********************************/

#include "d1_cards_jinc"

int StartingConditional()
{
    object oSpeaker = GetPCSpeaker();

    int nCard;

    while (!GetCardsInDeck (++nCard, oSpeaker) && nCard < CARD_MAX_ID)
        continue;

    return nCard < CARD_MAX_ID;
}
