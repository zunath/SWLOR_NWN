/**********************************/
/*          d1_gameowner_a15
/**********************************/
/*  Gives a deck bag.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    if (GetGold (GetPCSpeaker()) >= 2 || GetIsDebug())
    {
        CreateItemOnObject ("deckbag", GetPCSpeaker());

        if (!GetIsDebug())
            TakeGoldFromCreature (2, GetPCSpeaker());
    }
}
