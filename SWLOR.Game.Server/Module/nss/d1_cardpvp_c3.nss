
#include "d1_cards_jinc"

int StartingConditional()
{

    // enter opponent
    object oPC = GetPCSpeaker();
    int nSelection = GetMenuSelection();
    object oOpponent = GetMenuObjectValue (nSelection - 1);
    SetLocalObject(oPC, "CARD_OPPONENT", oOpponent);


    int nNth;
    int nScan = 1;
    int nSize = 9;
    int nCycle = GetMenuCycleTotal();

    object oPlayer = GetPCSpeaker();


    object oInv = GetFirstItemInInventory (oPlayer);
    int nDeckCount = 0;
    while (oInv != OBJECT_INVALID)
    {
        string sTag = GetTag (oInv);
        if (GetStringLeft (sTag, 13) == "CreaturesDeck")
        {
          if( (nNth <= nSize + nCycle) && (nNth >= nCycle))
          {
            int nCards = GetCardItemsInDeck(oInv);
            SetCustomToken (660 + nDeckCount, " Play with " + GetName(oInv) + " (" + IntToString(nCards) + " cards)");

            SetMenuObjectValue (oInv, nDeckCount, 1);
            SetMenuValue (nDeckCount + 1, nDeckCount);
            nDeckCount++;
          }
          nNth++;
        }
        oInv = GetNextItemInInventory (oPlayer);
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

    if (nDeckCount > nSize)
    {
        SetMenuHasMore (TRUE);
        SetMenuCycle (nNth);
    }
    else
        SetMenuHasMore (FALSE);

    return TRUE;
}

