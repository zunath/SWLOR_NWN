/**********************************/
/*          d_card_sac1c
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 15th November 2003
/**********************************/
/*  Checks if the option should be
/*  shown.
/**********************************/

int StartingConditional()
{
    return GetLocalObject (OBJECT_SELF, "CARD_SACRIFICE_OBJECT1") != OBJECT_INVALID;
}
