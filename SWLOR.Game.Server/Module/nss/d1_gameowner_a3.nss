/**********************************/
/*           d1_gameowner_a3
/**********************************/
/*  Purchases five new cards.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    object oPC = GetPCSpeaker();

    if (!GetIsDebug())
        TakeGoldFromCreature (50, oPC);

    ActionPurchaseCards (oPC, 5);
}
