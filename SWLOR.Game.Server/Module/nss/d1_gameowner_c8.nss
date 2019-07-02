/**********************************/
/*          d1_gameowner_c8
/**********************************/
/*  Checks if the player has no cards
/*  in their collection.
/**********************************/

#include "d1_cards_jinc"

int StartingConditional()
{
    object oSpeaker = GetPCSpeaker();

    int nCard;

    while (!GetCardsInDeck (++nCard, oSpeaker) && nCard < CARD_MAX_ID)
        continue;

    if(nCard >= CARD_MAX_ID)
      return TRUE;
    return FALSE;
}

