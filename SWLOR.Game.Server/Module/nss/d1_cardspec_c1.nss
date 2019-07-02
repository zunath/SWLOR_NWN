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

    object oPlayer, oOpponent;


    object oArea = GetObjectByTag ("CardGame");
    int nAreaCount = 0;
    int nTotalArea;
    while (oArea != OBJECT_INVALID)
    {
        if (GetCardGameToggle (oArea))
        {
          if( (nNth <= nSize + nCycle) && (nNth >= nCycle))
          {
            oPlayer = GetCardGamePlayer (1, oArea);
            oOpponent = GetCardGamePlayer (2, oArea);
            SetCustomToken (660 + nNth, GetName(oPlayer) + " versus " + GetName(oOpponent));
            if(GetIsPC(oPlayer))
            {
              SetMenuObjectValue (oPlayer, nAreaCount, 1);
            }else{
              SetMenuObjectValue (oOpponent, nAreaCount, 1);
            }
            SetMenuValue (nAreaCount + 1, nAreaCount);
            nAreaCount++;
          }
          nNth++;
        }
        nTotalArea++;
        oArea = GetObjectByTag ("CardGame", nTotalArea);
    }



    while (nNth <= nSize)
    {
        SetCustomToken (660 + nNth, "");
        SetMenuValue (0, nNth, 1);
        SetMenuValue (0, nNth, 2);
        nNth += 1;
    }


    SetMenuSize (nSize);
    SetMenuValueAmount (1);

    if (nAreaCount > nSize)
    {
        SetMenuHasMore (TRUE);
        SetMenuCycle (nNth);
    }
    else
        SetMenuHasMore (FALSE);

    return TRUE;
}
