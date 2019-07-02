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


    object oOpponent = GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC);
    int nPlayerCount = 0;
    int nTotalPlayer = 1;
    while (oOpponent != OBJECT_INVALID)
    {
        if (GetLocalInt(oOpponent, "WAGER") > 0)
        {
          if( (nNth <= nSize + nCycle) && (nNth >= nCycle))
          {
            string sMessage = "Play with " + GetName(oOpponent);
            int nGold = GetLocalInt(oOpponent, "WAGER_GOLD");
            int nAnte = GetLocalInt(oOpponent, "WAGER_ANTE");
            if((nGold == 0) && (nAnte == 0))
              sMessage = sMessage + " for fun.";
            if((nGold > 0) && (nAnte == 0))
              sMessage = sMessage + " for " + IntToString(nGold) + " gold pieces.";
            if((nAnte > 0) && (nGold == 0))
              sMessage = sMessage + " for ante.";
            if((nGold > 0) && (nAnte > 0))
              sMessage = sMessage + " for ante and " + IntToString(nGold) + " gold pieces.";

            SetCustomToken (660 + nPlayerCount, sMessage);

            SetMenuObjectValue (oOpponent, nPlayerCount, 1);
            SetMenuValue (nPlayerCount + 1, nPlayerCount);
            nPlayerCount++;
          }
          nNth++;
        }
        nTotalPlayer++;
        oOpponent = GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC, OBJECT_SELF, nTotalPlayer);
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

    if (nPlayerCount > nSize)
    {
        SetMenuHasMore (TRUE);
        SetMenuCycle (nNth);
    }
    else
        SetMenuHasMore (FALSE);

    return TRUE;
}
