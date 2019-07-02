/**********************************/
/*          d_card_sacpa
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 15th November 2003
/**********************************/
/*  Reduces cycle value by 9.
/**********************************/

void main()
{
    SetLocalInt (OBJECT_SELF, "CARD_SACRIFICE_CYCLE", GetLocalInt (OBJECT_SELF, "CARD_SACRIFICE_CYCLE") - 9);

}
