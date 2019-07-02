/**********************************/
/*          d1_cards_jmenu
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Telarnia.com
/*  Created On: God knows
/**********************************/
/*  This is a generic menu handling
/*  system for conversation files,
/*  where a dynamic menu is desired.
/**********************************/
/*  YOU DO NOT NEED TO ALTER ANY
/*  OF THIS TO ADD NEW CARDS
/**********************************/

void ClearMenu()
{
    int nNth1, nNth2;
    int nMax1 = GetMenuSize();
    int nMax2 = GetMenuValueAmount();

    SetMenuCycle (0);
    SetMenuCycleBack (0);
    SetMenuCycleTotal (0);
    SetMenuHasMore (FALSE);

    for (nNth1 = 0; nNth1 <= nMax1; nNth1++)
        for (nNth2 = 1; nNth2 <= nMax2; nNth2++)
            SetMenuValue (0, nNth1, nNth2);

    for (nNth1 = 1; nNth1 <= nMax2; nNth1++)
        SetMenuSelection (0, nNth1);

    SetMenuSize (0);

    if (nMax2 > 1)
        SetMenuValueAmount (0);
}

void SetMenuCycle (int nValue)
{
    if (!nValue)
        DeleteLocalInt (OBJECT_SELF, "GENERIC_MENU_OVERLAP");
    else
        SetLocalInt (OBJECT_SELF, "GENERIC_MENU_OVERLAP", nValue);
}

void SetMenuCycleBack (int nValue)
{
    int nNth;

    while (GetLocalInt (OBJECT_SELF, "GENERIC_MENU_LESS_" + IntToString (++nNth)))
        continue;

    if (!nValue)
    {
        while (--nNth > 0)
            DeleteLocalInt (OBJECT_SELF, "GENERIC_MENU_LESS_" + IntToString (nNth));
    }
    else if (nValue == -1)
        DeleteLocalInt (OBJECT_SELF, "GENERIC_MENU_LESS_" + IntToString (nNth - 1));
    else
        SetLocalInt (OBJECT_SELF, "GENERIC_MENU_LESS_" + IntToString (nNth), nValue);
}

void SetMenuCycleTotal (int nValue)
{
    if (!nValue)
        DeleteLocalInt (OBJECT_SELF, "GENERIC_MENU_TOTAL");
    else
        SetLocalInt (OBJECT_SELF, "GENERIC_MENU_TOTAL", nValue);
}

void SetMenuHasMore (int nMore = TRUE)
{
    if (!nMore)
        DeleteLocalInt (OBJECT_SELF, "GENERIC_MENU_MORE");
    else
        SetLocalInt (OBJECT_SELF, "GENERIC_MENU_MORE", nMore);
}

void SetMenuObjectValue (object oObject, int nMenuOption, int nID = 1)
{
    if (oObject == OBJECT_INVALID)
        DeleteLocalObject (OBJECT_SELF, "GENERIC_MENU_DATA_" + IntToString (nMenuOption) + "_" + IntToString (nID));
    else
        SetLocalObject (OBJECT_SELF, "GENERIC_MENU_DATA_" + IntToString (nMenuOption) + "_" + IntToString (nID), oObject);
}


void SetMenuSelection (int nValue, int nID = 1)
{
    if (!nValue)
        DeleteLocalInt (OBJECT_SELF, "GENERIC_MENU_SELECT_" + IntToString (nID));
    else
        SetLocalInt (OBJECT_SELF, "GENERIC_MENU_SELECT_" + IntToString (nID), nValue);
}

void SetMenuSize (int nSize = 9)
{
    if (!nSize)
        DeleteLocalInt (OBJECT_SELF, "GENERIC_MENU_SIZE");
    else
        SetLocalInt (OBJECT_SELF, "GENERIC_MENU_SIZE", nSize);
}

void SetMenuValue (int nValue, int nMenuOption, int nID = 1)
{
    if (!nValue)
        DeleteLocalInt (OBJECT_SELF, "GENERIC_MENU_DATA_" + IntToString (nMenuOption) + "_" + IntToString (nID));
    else
        SetLocalInt (OBJECT_SELF, "GENERIC_MENU_DATA_" + IntToString (nMenuOption) + "_" + IntToString (nID), nValue);
}

void SetMenuValueAmount (int nAmount = 1)
{
    if (!nAmount)
        DeleteLocalInt (OBJECT_SELF, "GENERIC_MENU_AMOUNT");
    else
        SetLocalInt (OBJECT_SELF, "GENERIC_MENU_AMOUNT", nAmount);
}

int GetMenuCycle()
{
    return GetLocalInt (OBJECT_SELF, "GENERIC_MENU_OVERLAP");
}

int GetMenuCycleBack()
{
    int nNth;

    while (GetLocalInt (OBJECT_SELF, "GENERIC_MENU_LESS_" + IntToString (++nNth)))
        continue;

    return GetLocalInt (OBJECT_SELF, "GENERIC_MENU_LESS_" + IntToString (nNth - 1));
}

int GetMenuCycleTotal()
{
    return GetLocalInt (OBJECT_SELF, "GENERIC_MENU_TOTAL");
}

int GetMenuHasMore()
{
    return GetLocalInt (OBJECT_SELF, "GENERIC_MENU_MORE");
}

object GetMenuObjectValue (int nMenuOption, int nID = 1)
{
    return GetLocalObject (OBJECT_SELF, "GENERIC_MENU_DATA_" + IntToString (nMenuOption) + "_" + IntToString (nID));
}

int GetMenuSelection (int nID = 1)
{
    return GetLocalInt (OBJECT_SELF, "GENERIC_MENU_SELECT_" + IntToString (nID));
}

int GetMenuSize()
{
    return GetLocalInt (OBJECT_SELF, "GENERIC_MENU_SIZE");
}

int GetMenuValue (int nMenuOption, int nID = 1)
{
    return GetLocalInt (OBJECT_SELF, "GENERIC_MENU_DATA_" + IntToString (nMenuOption) + "_" + IntToString (nID));
}

int GetMenuValueAmount()
{
    int nValue = GetLocalInt (OBJECT_SELF, "GENERIC_MENU_AMOUNT");

    return (nValue) ? nValue : 1;
}
