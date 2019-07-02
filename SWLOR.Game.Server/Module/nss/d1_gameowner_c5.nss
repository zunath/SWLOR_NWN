/**********************************/
/*          d1_gameowner_c5
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 25th February 2004
/**********************************/
/*  Sets values for selecting type
/*  of default rule to change.
/**********************************/

#include "d1_cards_jinc"

int StartingConditional()
{
    int nNth;

    while (nNth <= 5)
    {
        SetMenuValue (nNth + 1, nNth);

        nNth += 1;
    }

    SetMenuSize (5);

    SetCustomToken (670, PrintRules (GetDefaultRules()));

    return TRUE;
}
