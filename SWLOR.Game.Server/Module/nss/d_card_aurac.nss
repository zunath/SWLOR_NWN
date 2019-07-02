/**********************************/
/*          d_card_aurac
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 27th October 2003
/**********************************/
/*  Ends aura effects.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    object oExit = GetExitingObject();
    object oSelf = GetAreaOfEffectCreator();

    if (oExit == oSelf)
        return;

    ActionUseAura (oSelf, oExit, TRUE);
}
