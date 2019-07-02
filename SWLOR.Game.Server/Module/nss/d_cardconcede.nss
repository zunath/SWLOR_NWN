/**********************************/
/*          d_cardconcede
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/**********************************/
/*  Used when the Concede board is
/*  clicked on to end the game.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    object oArea = GetArea (OBJECT_SELF);
    object oUser = GetLastUsedBy();

    AssignCommand (oArea, ActionEndCardGame (CARD_GAME_END_CONCEDE, GetCardGamePlayerNumber (oUser), oArea));
}
