/**********************************/
/*          d_menu_sel4
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Telarnia.com
/*  Created On: God knows
/**********************************/
/*  Makes option selected value.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    int nMax = GetMenuValueAmount();
    int nNth;

    while (++nNth <= nMax)
        SetMenuSelection (GetMenuValue (4, nNth), nNth);
}
