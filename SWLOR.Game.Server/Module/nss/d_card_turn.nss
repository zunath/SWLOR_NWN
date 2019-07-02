/**********************************/
/*          d_card_turn
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 24th October 2003
/**********************************/
/*  This is the Game Turn script
/*  that executes to make each turn
/*  in the game occur.  Done
/*  seperately to avoid potential
/*  TMIs.  NOTE: MUST be run on the
/*  area that the game is occurring
/*  in.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    object oSearch = GetGameCentre (OBJECT_SELF);

    //Checks that a game is in progress
    if (!GetCardGameToggle (OBJECT_SELF)
        || GetAvatar (1, oSearch) == OBJECT_INVALID
        || GetAvatar (2, oSearch) == OBJECT_INVALID)
        return;

    if (GetDrawTerminate())
        return;

    int nTurn = GetGameTurn();

    ActionUpkeepCycle (++nTurn);

    SetGameTurn (nTurn);

    DelayCommand (CYCLE_TIME, ExecuteScript ("d_card_turn", OBJECT_SELF));
}

