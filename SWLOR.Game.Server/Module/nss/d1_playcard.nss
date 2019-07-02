/**********************************/
/*          d1_playcard
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 22nd October 2003
/**********************************/
/*  Plays the card when clicked on.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    if (GetOwner (OBJECT_SELF) == GetCardGamePlayerNumber (GetLastUsedBy()))
        ActionPlayCard (OBJECT_SELF);
}
