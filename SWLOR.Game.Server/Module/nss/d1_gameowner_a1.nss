/**********************************/
/*           d1_gameowner_a1
/**********************************/
/*  Purchases a deck
/**********************************/

#include "d1_cards_jinc"

void main()
{
    object oPC = GetPCSpeaker();

    // give them a card bag too.
    object oBag = GetCardBag(oPC, FALSE, TRUE);

    SetLocalInt (oPC, "PurchasedCards", TRUE);

    CreateItemOnObject ("creaturesrules", oPC);

    if (!GetIsDebug())
        TakeGoldFromCreature (10, oPC);

    ActionPurchaseCards (oPC, 40);
}
