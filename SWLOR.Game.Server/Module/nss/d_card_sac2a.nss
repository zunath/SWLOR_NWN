/**********************************/
/*          d_card_sac2a
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 15th November 2003
/**********************************/
/*  Sacrifices target.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    object oSacrifice = GetLocalObject (OBJECT_SELF, "CARD_SACRIFICE_OBJECT2");

    AssignCommand (oSacrifice, ActionUseSacrifice (GetOwner (oSacrifice)));
}
