/**********************************/
/*          d_card_auraa
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 27th October 2003
/**********************************/
/*  Starts an aura effect.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    object oEnter = GetEnteringObject();
    object oSelf = GetAreaOfEffectCreator();

    if (oEnter == oSelf)
        return;

    ActionUseAura (oSelf, oEnter);
}
