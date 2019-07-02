/**********************************/
/*          d_menu_smo1
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Telarnia.com
/*  Created On: God knows
/**********************************/
/*  Cycles forwards
/**********************************/

#include "d1_cards_jinc"

void main()
{
    SetMenuCycleBack (GetMenuCycleTotal());
    SetMenuCycleTotal (GetMenuCycle());
}
