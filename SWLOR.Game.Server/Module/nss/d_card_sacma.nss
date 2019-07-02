/**********************************/
/*          d_card_sacma
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 15th November 2003
/**********************************/
/*  Cycles the pages forward by 9,
/*  and clears the more flag.
/**********************************/

void main()
{
    SetLocalInt (OBJECT_SELF, "CARD_SACRIFICE_MORE", FALSE);
    SetLocalInt (OBJECT_SELF, "CARD_SACRIFICE_CYCLE", GetLocalInt (OBJECT_SELF, "CARD_SACRIFICE_CYCLE") + 9);
}
