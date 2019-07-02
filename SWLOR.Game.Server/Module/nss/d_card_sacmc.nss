/**********************************/
/*          d_card_sacmc
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 15th November 2003
/**********************************/
/*  Checks if the page has more
/*  options to show.
/**********************************/

int StartingConditional()
{
    return GetLocalInt (OBJECT_SELF, "CARD_SACRIFICE_MORE");
}
