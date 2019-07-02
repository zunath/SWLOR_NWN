/**********************************/
/*          d1_gameowner_c6
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 4th March 2004
/**********************************/
/*  Prints player record
/**********************************/

#include "d1_cards_jinc"

int StartingConditional()
{
    SetCustomToken (550, PrintPlayerRating (GetPCSpeaker()));

    return TRUE;
}
