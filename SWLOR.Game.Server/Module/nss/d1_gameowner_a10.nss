/**********************************/
/*          d1_gameowner_a10
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 24th February 2004
/**********************************/
/*  Sells individual card from
/*  player's collection.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    object oPC = GetPCSpeaker();

    struct sCard sInfo = GetCardInfo (GetMenuSelection (1));

    ActionTransferCard (sInfo.nCard, CARD_SOURCE_COLLECTION, CARD_SOURCE_ALL_CARDS, oPC, OBJECT_INVALID);
    GiveGoldToCreature (oPC, sInfo.nCost);
}
