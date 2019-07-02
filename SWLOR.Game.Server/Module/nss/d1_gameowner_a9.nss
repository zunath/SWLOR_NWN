/**********************************/
/*          d1_gameowner_a9
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 27th October 2003
/**********************************/
/*  Displays cards for sale.
/*  Adam: Added code for rarity.
/*  Jazael: Redone when menu system
/*          introduced.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    if (GetMenuCycleTotal() != GetMenuCycle())
        return;

    int nNth;
    int nSize = 9;
    int nRnd;

    object oSpeaker = GetPCSpeaker();

    struct sCard sSell;

    while (nNth <= nSize)
    {
        int nSell;
        int nAbort = 100;

        nRnd = Random (CARD_MAX_ID) + 1;

        if (nRnd >= CARD_MAX_ID)
            nRnd = CARD_MAX_ID - 1;

        sSell = GetCardInfo (nRnd);


        while (sSell.nCard < CARD_MAX_ID && !nSell && --nAbort)
        {
            if (GetRarityAllowed (sSell.nRarity))
                nSell = sSell.nCard;
            else
            {
                nRnd = Random (CARD_MAX_ID) + 1;

                if (nRnd >= CARD_MAX_ID)
                    nRnd = CARD_MAX_ID - 1;

                sSell = GetCardInfo (nRnd);
                if (GetRarityAllowed (sSell.nRarity))
                    nSell = sSell.nCard;
            }
        }

        if (nSell && (GetGold (oSpeaker) >= sSell.nCost * 2 || GetIsDebug()))
        {
            SetCustomToken (660 + nNth, "Buy " + sSell.sName + " (" + IntToString (GetCardsInDeck (sSell.nCard, oSpeaker)) + ") for " + IntToString (sSell.nCost * 2) + " gold.");
            SetMenuValue (sSell.nCard, nNth);
        }
        else
        {
            SetCustomToken (660 + nNth, "");
            SetMenuValue (0, nNth);
        }

        nNth += 1;
    }

    int nCycle = GetMenuCycle();

    SetMenuSize (nSize);
    SetMenuHasMore (TRUE);
    SetMenuCycle (++nCycle);
}
