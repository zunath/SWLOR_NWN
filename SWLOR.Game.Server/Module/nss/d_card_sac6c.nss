/**********************************/
/*          d_card_sac6c
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
    return GetLocalObject (OBJECT_SELF, "CARD_SACRIFICE_OBJECT6") != OBJECT_INVALID;
}
