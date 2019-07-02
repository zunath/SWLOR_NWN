/**********************************/
/*          d1_gameowner_a21
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 18th February 2004
/**********************************/
/*  Sells card.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    object oPC = GetPCSpeaker();

    struct sCard sSell = GetCardInfo (GetMenuSelection());

    int nNth;
    int nSize = GetMenuSize();

    while (nNth <= nSize && GetMenuValue (nNth) != sSell.nCard)
        nNth += 1;

    SetCustomToken (660 + nNth, "");
    SetMenuValue (0, nNth);

    ActionTransferCard (sSell.nCard, CARD_SOURCE_ALL_CARDS, CARD_SOURCE_COLLECTION, OBJECT_INVALID, oPC);
    AddToCardsSold();

    if (!GetIsDebug())
        TakeGoldFromCreature (sSell.nCost * 2, oPC);
}
