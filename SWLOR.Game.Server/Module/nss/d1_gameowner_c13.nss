/**********************************/
/*          d1_gameowner_c13
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 24th February 2004
/**********************************/
/*  Prints the card to be sold.
/**********************************/

#include "d1_cards_jinc"

int StartingConditional()
{
    struct sCard sInfo = GetCardInfo (GetMenuSelection (1));

    int nDeck = GetCardsInDeck (sInfo.nCard, GetPCSpeaker());

    SetCustomToken (675, sInfo.sName + " (" + IntToString (nDeck) + ") for " + IntToString (GetMenuSelection (2)));

    return nDeck;
}
