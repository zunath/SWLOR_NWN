/**********************************/
/*          d1_gameowner_c14
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 24th February 2004
/**********************************/
/*  Finds card to sell.
/**********************************/

#include "d1_cards_jinc"

int StartingConditional()
{
    int nNth;
    int nScan = 1;
    int nSize = 9;
    int nCycle = GetMenuCycleTotal();

    object oPlayer = GetPCSpeaker();

    struct sCard sInfo = GetCardInfo (nScan + nNth + nCycle);

    while (nNth <= nSize)
    {
        while (nScan < CARD_MAX_ID && !GetCardsInDeck (sInfo.nCard, oPlayer))
        {
            sInfo = GetCardInfo (++nScan + nNth + nCycle);
        }

        if (sInfo.sName != "")
        {
            SetCustomToken (660 + nNth, "Sell one " + sInfo.sName + " (" + IntToString (GetCardsInDeck (sInfo.nCard, oPlayer)) + ") for " + IntToString (sInfo.nCost));
            SetMenuValue (sInfo.nCard, nNth, 1);
            SetMenuValue (sInfo.nCost, nNth, 2);
        }
        else
        {
            SetCustomToken (660 + nNth, "");
            SetMenuValue (0, nNth, 1);
            SetMenuValue (0, nNth, 2);
        }

        nNth += 1;

        sInfo = GetCardInfo (nScan + nNth + nCycle);

    }

    SetMenuSize (nSize);
    SetMenuValueAmount (2);

    while (nScan < CARD_MAX_ID && !GetCardsInDeck (sInfo.nCard, oPlayer))
        sInfo = GetCardInfo (++nScan + nNth + nCycle);

    if (sInfo.sName != "")
    {
        SetMenuHasMore (TRUE);
        SetMenuCycle (sInfo.nCard);
    }
    else
        SetMenuHasMore (FALSE);

    return TRUE;
}
