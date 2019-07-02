/**********************************/
/*          d_card_sacpc
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 15th November 2003
/**********************************/
/*  Checks to see if the pages have
/*  been cycled.
/**********************************/

int StartingConditional()
{
    return GetLocalInt (OBJECT_SELF, "CARD_SACRIFICE_CYCLE");
}
